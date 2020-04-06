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
        private bool IsInitialized { get; set; }
        private MediaState CurrentMediaState { get; set; }
        internal MediaState? OverridingState { get; private set; }

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
                if (this.OverridingState == null)
                {
                    Task.Run(async () =>
                    {
                        await this.NotifyEndpoint();
                    });
                }
            }
        }

        internal void Override(MediaState newState)
        {
            this.OverridingState = newState;
            Task.Run(async () =>
            {
                await this.NotifyEndpoint();
            });
        }

        internal void OverrideAndWait(MediaState newState)
        {
            this.OverridingState = newState;
            Task.Run(async () =>
            {
                await this.NotifyEndpoint();
            }).Wait();
        }

        internal void StopOverriding()
        {
            this.OverridingState = null;
            Task.Run(async () =>
            {
                await this.NotifyEndpoint();
            });
        }

        internal async Task NotifyEndpoint()
        {
            if (!this.IsInitialized)
                throw new InvalidOperationException("API Endpoint must be initialized first.");

            string value = "";
            switch (this.OverridingState, this.CurrentMediaState)
            {
                case (MediaState.Playing, _):
                case (MediaState.Standby, _):
                    value = "force playing";
                    break;
                case (MediaState.Stopped, _):
                    value = "force stopped";
                    break;
                case (null, MediaState.Playing):
                case (null, MediaState.Standby):
                    value = "playing";
                    break;
                case (null, MediaState.Stopped):
                    value = "stopped";
                    break;
            }

            var payload = String.Format(@"{{ ""state"": ""{0}"" }}", value);

            var request = new HttpRequestMessage(HttpMethod.Post, this.Url);
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var whatImSending = await content.ReadAsStringAsync();
            var response = await Client.PostAsync(this.Url, content).ConfigureAwait(false);
        }
    }
}
