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

        public MainWindow()
        {
            InitializeComponent();
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
                    this.ApiEndpoint.NotifyEndpoint(currentState);
                    await this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.UpdateUi(currentState);
                        this.OverrideButton.IsEnabled = true;
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
    }
}
