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

        public MainWindow()
        {
            InitializeComponent();
            this.Configuration = new ConfigurationReader();
            this.Sensor = new Sensor();
            var apiEndpoint = new ApiEndpoint(this.Sensor, this.Configuration);
            this.Core = new Core(this.Configuration, apiEndpoint, this.Sensor);
            this.Core.StateUpdated += OnStateUpdated;
            Application.Current.SessionEnding += OnSessionEnding;
            this.Closing += OnWindowClosing;
            this.PreviewKeyDown += OnPreviewKeyDown;

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

            await this.Core.InitializeAsync();
            await this.Core.UpdateEndpointAsync();
        }

        /// <summary>
        /// Reacts to new state by updating the UI
        /// </summary>
        private void OnStateUpdated(object? sender, UpdateArgs e)
        {
            if (e.Exception == null)
                UpdateUi(e);
            else
                HandleException(e.Exception);
        }

        /// <summary>
        /// Updates the text labels in the UI. This method may be called from any thread.
        /// </summary>
        /// <param name="args"><see cref="UpdateArgs"/> which contain the updated state.</param>
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
                _ => "Media: error",
            };
            this.OverridingText.Text = args.OverrideState switch
            {
                TargetState.Off => "Switch: Off",
                TargetState.On => "Switch: On",
                TargetState.FromMedia => "Switch: Auto",
                TargetState.Shutdown => "Switch: On for just a minute",
                _ => "Switch: error",
            };

            this.StatusText.Visibility = this.Configuration.SoundSensor 
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Handle keyboard gestures, whether the button is focused or not
        /// </summary>
        private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Space:
                    // Space toggles the button
                    RunAsyncSafely(async () => await this.Core.ToggleOverrideAsync());
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Enter:
                    // Enter toggles the button and minimizes
                    RunAsyncSafely(async () => await this.Core.ToggleOverrideAsync());
                    this.WindowState = WindowState.Minimized;
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Escape:
                    // Escape minimizes
                    e.Handled = true;
                    this.WindowState = WindowState.Minimized;
                    break;

                default:
                    e.Handled = false;
                    break;
            }
        }

        /// <summary>
        /// Handle mouse gesture for the button
        /// </summary>
        private void OnOverrideButtonClick(object sender, RoutedEventArgs e)
        {
            RunAsyncSafely(async () => await this.Core.ToggleOverrideAsync());
        }

        /// <summary>
        /// Turn off the light after delay when user closes the app
        /// </summary>
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RunAsyncSafely(async () => await this.Core.ShutdownAsync())
                .Wait();
        }

        /// <summary>
        /// Turn off the light after delay when OS closes the app
        /// </summary>
        private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            RunAsyncSafely(async () => await this.Core.ShutdownAsync())
                .Wait();
        }

        /// <summary>
        /// Dispatches async method to run. Returns immediately. Handles exceptions.
        /// </summary>
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

        /// <summary>
        /// Handle exception by permanently disabling the sensor and displaying the message.
        /// </summary>
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
