using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MediaSensor
{
    /// <summary>
    /// Reports updates to API endpoint
    /// </summary>
    internal class ApiEndpoint
    {
        private Sensor Sensor { get; }
        public HttpClient Client { get; }
        private string Url { get; set;  }
        private string Token { get; set; }
        internal bool IsOverriding { get; private set; }
        private bool IsInitialized { get; set; }

        private MediaState CurrentMediaState;
        private MediaState OverridingState;

        internal ApiEndpoint(Sensor sensor)
        {
            if (sensor == null)
                throw new ArgumentNullException(nameof(sensor));

            this.Sensor = sensor;
            this.Client = new HttpClient();
        }

        internal void Initialize(ConfigurationReader configuration)
        {
            if (!configuration.Initialized)
                throw new InvalidOperationException("Configuration hasn't been initialized yet.");

            if (string.IsNullOrWhiteSpace(configuration.Url))
                throw new InvalidOperationException("Configuration contains invalid URL.");
            if (string.IsNullOrWhiteSpace(configuration.Token))
                throw new InvalidOperationException("Configuration contains invalid token.");

            this.Url = configuration.Url;
            this.Token = configuration.Token;
            this.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.Token);
            this.IsInitialized = true;
            this.Sensor.StateChanged += Sensor_StateChanged;
        }

        private void Sensor_StateChanged(object sender, SensorStateEventArgs e)
        {
            if (e.State != this.CurrentMediaState)
            {
                this.CurrentMediaState = e.State;
                if (!this.IsOverriding)
                {
                    this.NotifyEndpoint(this.CurrentMediaState);
                }
            }
        }

        internal void Override(MediaState overridingState)
        {
            this.IsOverriding = true;
            this.OverridingState = overridingState;

            if (overridingState != this.CurrentMediaState)
            {
                this.NotifyEndpoint(overridingState);
            }
        }

        internal void StopOverriding()
        {
            this.IsOverriding = false;
            if (this.CurrentMediaState != this.OverridingState)
            {
                this.NotifyEndpoint(this.CurrentMediaState);
            }
        }

        internal void NotifyEndpoint(MediaState currentMediaState)
        {
            if (!this.IsInitialized)
                throw new InvalidOperationException("API Endpoint must be initialized first.");

            string value = "";
            switch (currentMediaState)
            {
                case MediaState.Playing:
                case MediaState.Standby:
                    value = "playing";
                    break;
                case MediaState.Stopped:
                    value = "stopped";
                    break;
            }
            /*
            switch (this.IsOverriding, currentMediaState)
            {
                case (true, _):
                    break;
                case (false, MediaState.Playing):
                case (false, MediaState.Standby):
                    value = "playing";
                    break;
                case (false, MediaState.Stopped):
                    value = "stopped";
                    break;
            }
            */
            var payload = String.Format(@"{{ ""state"": ""{0}"" }}", value);

            var request = new HttpRequestMessage(HttpMethod.Post, this.Url);
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            Task.Run(async () =>
            {
                var whatImSending = await content.ReadAsStringAsync();
                var response = await Client.PostAsync(this.Url, content).ConfigureAwait(false);
            });
        }
    }
}
