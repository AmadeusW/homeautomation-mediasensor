using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

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
        }

        internal async Task NotifyEndpoint(TargetState state)
        {
            if (!this.IsInitialized)
                throw new InvalidOperationException("API Endpoint must be initialized first.");

            string value = state switch
            {
                TargetState.On => "playing",
                TargetState.Off => "stopped",
                TargetState.Shutdown => "shutdown",
                _ => "other",
            };

            var payload = String.Format(@"{{ ""state"": ""{0}"" }}", value);

            var request = new HttpRequestMessage(HttpMethod.Post, this.Url);
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var whatImSending = await content.ReadAsStringAsync();
            var response = await Client.PostAsync(this.Url, content).ConfigureAwait(false);
        }
    }
}
