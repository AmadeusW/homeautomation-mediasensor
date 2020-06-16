using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MediaSensor
{
    internal class Core
    {
        internal event EventHandler<UpdateArgs> StateUpdated;

        ApiEndpoint ApiEndpoint { get; }
        Sensor Sensor { get; }
        ConfigurationReader Configuration { get; }
        TargetState OverrideState { get; set; }
        MediaState MediaState { get; set; }

        public Core(ConfigurationReader configuration, ApiEndpoint apiEndpoint, Sensor sensor)
        {
            Configuration = configuration;
            ApiEndpoint = apiEndpoint;
            Sensor = sensor;

            OverrideState = Configuration.SoundSensor ? TargetState.FromMedia : TargetState.On;
            this.Sensor.StateChanged += Sensor_StateChanged;
        }

        internal async Task InitializeAsync()
        {
            try
            {
                this.Configuration.Initialize();
                if (this.Configuration.Initialized == false)
                {
                    // Configuration was just created. There is no point continuing.
                    throw new InvalidOperationException($"Please update {ConfigurationReader.ConfigurationFileName}");
                }
                this.ApiEndpoint.Initialize(this.Configuration);
                if (this.Configuration.SoundSensor)
                {
                    this.Sensor.Initialize(this.Configuration);

                    // Prepare for the initial request and UI update
                    this.MediaState = this.Sensor.CurrentState;

                    // Start regular updates from the sensor
                    this.Sensor.Start();
                }
                else
                {
                    // Start with the light on
                    this.OverrideState = TargetState.On;
                }

                this.UpdateUi();
                await this.UpdateEndpointAsync();
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }
        }

        private void Sensor_StateChanged(object sender, SensorStateEventArgs e)
        {
            SetMediaStateAsync(e.State);
        }

        internal async Task ShutdownAsync()
        {
            OverrideState = TargetState.Shutdown;

            this.UpdateUi();
            await this.UpdateEndpointAsync();
        }

        internal async Task SetMediaStateAsync(MediaState state)
        {
            if (state == this.MediaState)
                return;

            this.MediaState = state;

            this.UpdateUi();

            if (this.OverrideState == TargetState.FromMedia)
                await this.UpdateEndpointAsync();
        }

        internal async Task ToggleOverrideAsync()
        {
            OverrideState = OverrideState switch
            {
                TargetState.FromMedia => MediaState switch
                {
                    MediaState.Playing => TargetState.On,
                    MediaState.Standby => TargetState.On,
                    MediaState.Stopped => TargetState.Off,
                },
                // Without media sensor, we only have on and off states
                TargetState.On => Configuration.SoundSensor ? TargetState.FromMedia : TargetState.Off,
                TargetState.Off => Configuration.SoundSensor ? TargetState.FromMedia : TargetState.On,
            };

            this.UpdateUi();
            await this.UpdateEndpointAsync();
        }

        internal async Task UpdateEndpointAsync()
        {
            TargetState target = OverrideState switch
            {
                TargetState.On => TargetState.On,
                TargetState.Off => TargetState.Off,
                TargetState.Shutdown => TargetState.Shutdown,
                TargetState.FromMedia => MediaState switch
                {
                    MediaState.Playing => TargetState.Off, // volume greater than 0
                    MediaState.Standby => TargetState.Off, // volume smidgen
                    MediaState.Stopped => TargetState.On, // volume 0
                },
            };

            await ApiEndpoint.NotifyEndpoint(target);
        }

        private void UpdateUi()
        {
            var args = new UpdateArgs(this.MediaState, this.OverrideState, exception: null);
            this.StateUpdated?.Invoke(this, args);
        }

        private void HandleException(Exception ex)
        {
            // Stop the updates
            this.Sensor.Stop();
            var args = new UpdateArgs(this.MediaState, this.OverrideState, exception: ex);
            StateUpdated?.Invoke(this, args);
        }
    }
}
