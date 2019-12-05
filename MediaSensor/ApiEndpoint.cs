using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaSensor
{
    /// <summary>
    /// Reports updates to API endpoint
    /// </summary>
    internal class ApiEndpoint
    {
        private string Url { get; }
        private string Token { get; }
        internal bool Active { get; set; }
        internal bool IsOverriding { get; private set; }


        private MediaState CurrentMediaState;
        private MediaState OverridingState;

        internal ApiEndpoint(string url, string token, Sensor sensor)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));
            if (sensor == null)
                throw new ArgumentNullException(nameof(sensor));

            this.Url = url;
            this.Token = token;
            this.Active = true;
            sensor.StateChanged += Sensor_StateChanged;
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

        private void NotifyEndpoint(MediaState currentMediaState)
        {
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
            var payload = string.Format("{\"state\": \"{0}\"}", value);

            var request = WebRequest.CreateHttp(this.Url);
            request.Method = "POST";
            request.Headers.Add(HttpRequestHeader.Authorization, this.Token);
            request.ContentType = "application/json";
            request.ContentLength = payload.Length;
            var response = request.GetResponse();
        }
    }
}
