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
        private Core Core { get; set; }

        private bool ShowMediaState { get; set; }
        private bool ShowOverrideState { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            OverridePanel.Visibility = Visibility.Hidden;
            var configuration = new ConfigurationReader();
            var sensor = new Sensor();
            var apiEndpoint = new ApiEndpoint(sensor);
            this.Core = new Core(configuration, apiEndpoint, sensor);
            this.Core.StateUpdated += OnStateUpdated;

            ShowMediaState = configuration.SoundSensor;
            ShowOverrideState = false; // until user toggles it

            Task.Run(async () =>
            {
                await this.Core.InitializeAsync();
                await this.Core.UpdateEndpointAsync();
            });
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
            this.StatusText.Visibility = Visibility.Visible;
            this.StatusText.Text = ex.Message;
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                await this.Core.ToggleOverrideAsync();
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Task.Run(async () =>
            {
                await this.Core.ShutdownAsync();
            }).Wait();
        }
    }
}
