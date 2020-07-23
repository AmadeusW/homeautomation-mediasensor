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
        private WindowState LastWindowState { get; set; }
        private bool CanToggleOnRestore { get; set; }
        private static TimeSpan ToggleOnRestoreDelay { get; } = TimeSpan.FromSeconds(10);

        public MainWindow()
        {
            InitializeComponent();
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

            await this.Core.InitializeAsync();
            await this.Core.UpdateEndpointAsync();

            this.StateChanged += OnStateChanged;
            this.CanToggleOnRestore = true;
        }

        private void OnStateChanged(object? sender, EventArgs e)
        {
            if (this.LastWindowState == WindowState.Minimized
                && this.WindowState != WindowState.Minimized)
            {
                OnWindowRestored();
            }
            this.LastWindowState = this.WindowState;
        }

        private void OnWindowRestored()
        {
            if (this.Configuration.ToggleOnRestore && this.CanToggleOnRestore)
            {
                RunAsyncSafely(async () =>
                {
                    this.CanToggleOnRestore = false;
                    await this.Core.ToggleOverrideAsync();
                    _ = this.Dispatcher.BeginInvoke((Action)(async () => await this.AnimateAndMinimize()));
                    await Task.Delay(ToggleOnRestoreDelay);
                    this.CanToggleOnRestore = true;
                });
            }
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

            this.StatusText.Visibility = this.ShowMediaState ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            RunAsyncSafely(async () => await this.Core.ToggleOverrideAsync());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RunAsyncSafely(async () => await this.Core.ShutdownAsync())
                .Wait();
        }

        private async Task AnimateAndMinimize()
        {
            var textToRestore = this.OverridingText.Text;
            this.OverridingText.Text = "Gotcha!  ";
            await Task.Delay(300).ConfigureAwait(true);
            this.OverridingText.Text = "Gotcha!! ";
            await Task.Delay(300).ConfigureAwait(true);
            this.OverridingText.Text = "Gotcha!!!";
            await Task.Delay(300).ConfigureAwait(true);
            this.OverridingText.Text = textToRestore;
            this.WindowState = WindowState.Minimized;
        }

        private Task RunAsyncSafely(Func<Task> action)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    this.HandleException(ex);
                }
            });
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

            // If possible, update the UI
            this.StatusText.Text = ex.Message;
        }
    }
}
