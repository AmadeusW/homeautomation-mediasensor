using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaSensor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal ConfigurationReader Configuration { get; }
        private Sensor Sensor { get; }
        private ApiEndpoint ApiEndpoint { get; }
        private const string configurationFileName = "mediasensor.yaml";
        private OverrideStatus OverrideToggleStatus = OverrideStatus.NotOverriding;

        public MainWindow()
        {
            InitializeComponent();
            OverridePanel.Visibility = Visibility.Hidden;
            this.Configuration = new ConfigurationReader(configurationFileName);
            this.Sensor = new Sensor();
            ApiEndpoint = new ApiEndpoint(this.Sensor);
            Task.Run(async () =>
            {
                try
                {
                    this.Configuration.Initialize();
                    if (this.Configuration.Initialized == false)
                    {
                        // Configuration was just created. There is no point continuing.
                        throw new InvalidOperationException($"Please update {configurationFileName}");
                    }
                    this.ApiEndpoint.Initialize(this.Configuration);
                    ConnectUiUpdates();

                    // Set initial UI and make initial request
                    var currentState = this.Sensor.CurrentState;
                    this.ApiEndpoint.NotifyEndpoint();
                    await this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.UpdateUi(currentState);
                        OverridePanel.Visibility = Visibility.Visible;
                    }));

                    // Now that everything is fine, let's start the regular updates from the sensor.
                    this.Sensor.Start();
                }
                catch (Exception ex)
                {
                    await this.Dispatcher.BeginInvoke((Action)(() => { this.HandleException(ex); }));
                }
            });
        }

        private void Sensor_StateChanged(object sender, SensorStateEventArgs e)
        {
            // Switch to UI thread
            this.Dispatcher.BeginInvoke((Action)(() => { this.UpdateUi(e.State); }));
        }

        private void UpdateUi(MediaState state)
        {
            switch (state)
            {
                case MediaState.Stopped:
                    this.StatusText.Text = "Media: Stopped";
                    break;
                case MediaState.Standby:
                    this.StatusText.Text = "Media: Standby";
                    break;
                case MediaState.Playing:
                    this.StatusText.Text = "Media: Playing";
                    break;
            }
        }

        private void HandleException(Exception ex)
        {
            this.StatusText.Text = ex.Message;
            this.Sensor.Stop();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Minimized:
                    DisconnectUiUpdates();
                    return;
                default:
                    UpdateUi(Sensor.CurrentState);
                    ConnectUiUpdates();
                    return;
            }
        }

        private void ConnectUiUpdates()
        {
            Sensor.StateChanged += Sensor_StateChanged;
        }

        private void DisconnectUiUpdates()
        {
            Sensor.StateChanged -= Sensor_StateChanged;
        }

        private enum OverrideStatus
        {
            NotOverriding,              // Go to OverridingPlaying or OverridingStopped, depending on current state
            OverridingPlaying,          // go to next state
            InverseOfOverridingPlaying, // go to NotOverriding
            OverridingStopped,          // go to next state
            InverseOfOverridingStopped  // go to NotOverriding
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            // Advance the state machine
            switch (this.OverrideToggleStatus)
            {
                case OverrideStatus.NotOverriding:
                    this.OverrideToggleStatus
                        = (this.Sensor.CurrentState == MediaState.Playing || this.Sensor.CurrentState == MediaState.Standby)
                        ? OverrideStatus.OverridingPlaying
                        : OverrideStatus.OverridingStopped;
                    break;
                case OverrideStatus.OverridingPlaying:
                    this.OverrideToggleStatus = OverrideStatus.InverseOfOverridingPlaying;
                    break;
                case OverrideStatus.OverridingStopped:
                    this.OverrideToggleStatus = OverrideStatus.InverseOfOverridingStopped;
                    break;
                case OverrideStatus.InverseOfOverridingPlaying:
                case OverrideStatus.InverseOfOverridingStopped:
                    this.OverrideToggleStatus = OverrideStatus.NotOverriding;
                    break;
            }

            // Act
            switch (this.OverrideToggleStatus)
            {
                case OverrideStatus.NotOverriding:
                    this.ApiEndpoint.StopOverriding();
                    this.OverridingText.Text = "Override: No";
                    break;
                case OverrideStatus.OverridingPlaying:
                case OverrideStatus.InverseOfOverridingStopped:
                    this.ApiEndpoint.Override(MediaState.Stopped); 
                    this.OverridingText.Text = "Override: Stopped";
                    break;
                case OverrideStatus.OverridingStopped:
                case OverrideStatus.InverseOfOverridingPlaying:
                    this.ApiEndpoint.Override(MediaState.Playing);
                    this.OverridingText.Text = "Override: Playing";
                    break;
            }
        }
    }
}
