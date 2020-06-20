using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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
        private ConfigurationReader Configuration { get; }

        private HttpClient? Client { get; set;  }
        private string? Url { get; set; }
        private string? Token { get; set; }

        private bool isInitialized = false;
        private object initializationLock = new object();

        internal ApiEndpoint(Sensor sensor, ConfigurationReader configuration)
        {
            if (sensor == null)
                throw new ArgumentNullException(nameof(sensor));

            this.Sensor = sensor;
            this.Configuration = configuration;
        }

        internal async Task NotifyEndpoint(string stateName)
        {
            EnsureInitialized();

            var payload = String.Format(@"{{ ""state"": ""{0}"" }}", stateName);

            var request = new HttpRequestMessage(HttpMethod.Post, this.Url);
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
#if DEBUG
            var whatImSending = await content.ReadAsStringAsync();
#endif
            var response = await Client!.PostAsync(this.Url, content).ConfigureAwait(false);
        }

        private void EnsureInitialized()
        {
            lock (initializationLock)
            {
                if (this.isInitialized)
                    return;

                if (!this.Configuration.Initialized)
                    throw new InvalidOperationException("Configuration hasn't been initialized yet.");

                if (string.IsNullOrWhiteSpace(this.Configuration.Url))
                    throw new InvalidOperationException("Configuration contains invalid URL.");
                if (string.IsNullOrWhiteSpace(this.Configuration.Token))
                    throw new InvalidOperationException("Configuration contains invalid token.");

                this.Url = this.Configuration.Url;
                this.Token = this.Configuration.Token;
                this.Client = new HttpClient();
                this.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.Token);
                this.isInitialized = true;
            }
        }
    }
}
