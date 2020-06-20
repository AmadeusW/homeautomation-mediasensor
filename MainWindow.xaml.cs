using System;
using System.Threading.Tasks;
using System.Windows;

namespace MediaSensor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Core Core { get; }
        private ConfigurationReader Configuration { get; }
        private Sensor Sensor { get; }

        private bool ShowMediaState { get; set; }
        private bool ShowOverrideState { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            OverridePanel.Visibility = Visibility.Hidden;
            this.Configuration = new ConfigurationReader();
            this.Sensor = new Sensor();
            var apiEndpoint = new ApiEndpoint(this.Sensor, this.Configuration);
            this.Core = new Core(this.Configuration, apiEndpoint, this.Sensor);
            this.Core.StateUpdated += OnStateUpdated;

            Task.Run(async () =>
            {
                try
                {
                    await this.InitializeAsync();
                }
                catch (Exception ex)
                {
                    this.HandleException(ex);
                }
            });
        }

        private async Task InitializeAsync()
        {
            this.Configuration.Initialize();
            if (this.Configuration.Initialized == false)
            {
                // Configuration was just created. There is no point continuing.
                throw new InvalidOperationException($"Please update {ConfigurationReader.ConfigurationFileName}");
            }

            ShowMediaState = this.Configuration.SoundSensor;
            ShowOverrideState = true;

            await this.Core.InitializeAsync();
            await this.Core.UpdateEndpointAsync();
        }

        private void OnStateUpdated(object? sender, UpdateArgs e)
        {
            if (e.Exception == null)
                UpdateUi(e);
            else
                HandleException(e.Exception);
        }

        private void UpdateUi(UpdateArgs args)
        {
            if (System.Windows.Threading.Dispatcher.CurrentDispatcher != this.Dispatcher)
            {
                this.Dispatcher.BeginInvoke((Action)(() => this.UpdateUi(args)));
                return;
            }

            this.StatusText.Text = args.MediaState switch
            {
                MediaState.Stopped => "Media: Stopped",
                MediaState.Standby => "Media: Standby",
                MediaState.Playing => "Media: Playing",
            };
            this.OverridingText.Text = args.OverrideState switch
            {
                TargetState.Off => "Switch: Off",
                TargetState.On => "Switch: On",
                TargetState.FromMedia => "Switch: Auto",
                TargetState.Shutdown => "Switch: On for just a minute",
            };

            if (args.OverrideState != TargetState.FromMedia)
            {
                this.ShowOverrideState = true;
            }

            this.OverridePanel.Visibility = this.ShowOverrideState ? Visibility.Visible : Visibility.Collapsed;
            this.StatusText.Visibility = this.ShowMediaState ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HandleException(Exception ex)
        {
            // Immediately stop further updates
            this.Sensor.Stop();

            // Switch to UI thread if needed
            if (System.Windows.Threading.Dispatcher.CurrentDispatcher != this.Dispatcher)
            {
                this.Dispatcher.BeginInvoke((Action)(() => this.HandleException(ex)));
                return;
            }

            // Update UI
            this.OverridePanel.Visibility = Visibility.Collapsed;
            this.StatusText.Text = ex.Message;
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Core.ToggleOverrideAsync();
                }
                catch (Exception ex)
                {
                    this.HandleException(ex);
                }
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Core.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    this.HandleException(ex);
                }
            }).Wait();
        }
    }
}
