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
            this.Sensor.StateChanged += Sensor_StateChanged;
        }

        internal async Task InitializeAsync()
        {
            if (this.Configuration.SoundSensor)
            {
                this.Sensor.Initialize(this.Configuration);

                // Start with the light tuned to the music
                this.MediaState = this.Sensor.CurrentState;
                this.OverrideState = TargetState.FromMedia;

                // Begin regular updates from the sensor
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
            string stateName = OverrideState switch
            {
                TargetState.On => "on switch",
                TargetState.Off => "off switch",
                TargetState.Shutdown => "shutdown",
                TargetState.FromMedia => MediaState switch
                {
                    MediaState.Playing => "off media", // volume greater than 0
                    MediaState.Standby => "off media", // volume smidgen
                    MediaState.Stopped => "on media", // volume 0
                },
            };

            await ApiEndpoint.NotifyEndpoint(stateName);
        }

        private void Sensor_StateChanged(object sender, SensorStateEventArgs e)
        {
            SetMediaStateAsync(e.State);
        }

        private void UpdateUi()
        {
            var args = new UpdateArgs(this.MediaState, this.OverrideState, exception: null);
            this.StateUpdated?.Invoke(this, args);
        }
    }
}
