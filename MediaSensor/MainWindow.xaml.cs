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
        private Sensor Sensor { get; }
        public MainWindow()
        {
            InitializeComponent();
            Sensor = new Sensor();
            ConnectUiUpdates();
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
                    this.StatusText.Text = "Stopped";
                    break;
                case MediaState.Standby:
                    this.StatusText.Text = "Standby";
                    break;
                case MediaState.Playing:
                    this.StatusText.Text = "Playing";
                    break;
            }
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
