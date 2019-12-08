using System;
using System.Diagnostics;
using System.IO;

namespace MediaSensor
{
    class ConfigurationReader
    {
        private readonly string configurationFileName;
        internal string Url { get; private set; }
        internal string Token { get; private set; }
        internal bool Initialized { get; private set; }

        internal ConfigurationReader(string configurationFileName)
        {
            if (String.IsNullOrWhiteSpace(configurationFileName))
            {
                throw new ArgumentNullException(nameof(configurationFileName));
            }
            this.configurationFileName = configurationFileName;
        }

        internal void Initialize()
        {
            if (Initialized)
                return;

            if (File.Exists(this.configurationFileName))
            {
                ReadConfiguration();
                Initialized = true;
            }
            else
            {
                File.WriteAllText(this.configurationFileName,
@"url: 192.168.0.0:1234/api/name # URL of the API endpoint, starting with http://
token: asdf # Home Assistant long term token
");
                Initialized = false;
            }
        }

        private bool ReadConfiguration()
        {
            bool gotUrl = false; 
            bool gotToken = false;
            var lines = File.ReadAllLines(this.configurationFileName);
            foreach (var line in lines)
            {
                var (key, rest) = GetBeforeAndAfter(line, ':');
                var (value, comment) = GetBeforeAndAfter(rest, '#');
                switch (key)
                {
                    case "url":
                        Url = value;
                        gotUrl = true;
                        break;
                    case "token":
                        Token = value;
                        gotToken = true;
                        break;
                }

                if (gotUrl && gotToken)
                    return true;
            }
            throw new Exception("Configuration did not contain url or token");
        }

        private (string, string) GetBeforeAndAfter(string text, char separator)
        {
            var separatorIndex = text.IndexOf(separator);
            if (separatorIndex == -1)
                return (text, string.Empty);

            var before = separator > 0 ? text.Substring(0, separatorIndex) : string.Empty;
            var after = separator + 1 < text.Length ? text.Substring(separatorIndex + 1) : string.Empty;
            return (before.Trim(), after.Trim());
        }
    }
}
