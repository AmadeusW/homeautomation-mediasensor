using System;
using System.Diagnostics;
using System.IO;

namespace MediaSensor
{
    class ConfigurationReader
    {
        internal const string ConfigurationFileName = "mediasensor.yaml";
        internal string Url { get; private set; }
        internal string Token { get; private set; }
        internal int Poll { get; private set; }
        internal int Latch { get; private set; }
        internal bool Initialized { get; private set; }
        internal bool SoundSensor { get; private set; }
        internal bool ToggleOnRestore { get; private set; }

        internal ConfigurationReader()
        {
        }

        internal void Initialize()
        {
            if (Initialized)
                return;

            if (File.Exists(ConfigurationFileName))
            {
                ReadConfiguration();
                Initialized = true;
            }
            else
            {
                // Produce a sample configuration
                File.WriteAllText(ConfigurationFileName,
@"url: http://host:8123/api/states/sensor.media # URL of the API endpoint. See https://developers.home-assistant.io/docs/en/external_api_rest.html
token: InsertLongTermTokenHere # Home Assistant long term token
poll: 250 # Polling delay in milliseconds. This represents delay between calls to the OS.
latch: 1000 # Latching delay in milliseconds. This represents duration of how long media state must be steady before making API call 
soundsensor: true # true to use sound sensor. false to use the app as on-off switch
onrestore: true # true to toggle on restore. false to not react to window restore
");
                Initialized = false;
            }
        }

        private bool ReadConfiguration()
        {
            bool gotUrl, gotToken, gotPoll, gotLatch, gotSoundSensor, gotRestore;
            gotUrl = gotToken = gotPoll = gotLatch = gotSoundSensor = gotRestore = false;

            var lines = File.ReadAllLines(ConfigurationFileName);
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
                    case "soundsensor":
                        SoundSensor = Boolean.Parse(value);
                        gotSoundSensor = true;
                        break;
                    case "poll":
                        Poll = Int32.Parse(value);
                        gotPoll = true;
                        break;
                    case "latch":
                        Latch = Int32.Parse(value);
                        gotLatch = true;
                        break;
                    case "onrestore":
                        ToggleOnRestore = Boolean.Parse(value);
                        gotRestore = true;
                        break;
                }

                if (gotUrl && gotToken && gotPoll && gotLatch && gotSoundSensor && gotRestore)
                    return true;
            }

            if (!gotUrl)
                throw new ApplicationException("Configuration did not contain key: url");
            if (!gotToken)
                throw new ApplicationException("Configuration did not contain key: token");
            if (!gotPoll)
                throw new ApplicationException("Configuration did not contain key with integer value: poll");
            if (!gotLatch)
                throw new ApplicationException("Configuration did not contain key with integer value: latch");
            if (!gotSoundSensor)
                throw new ApplicationException("Configuration did not contain key with Boolean value: soundsensor");
            if (!gotRestore)
                throw new ApplicationException("Configuration did not contain key with Boolean value: onrestore");
            return false;
        }

        private (string, string) GetBeforeAndAfter(string text, char separator)
        {
            var separatorIndex = text.IndexOf(separator);
            if (separatorIndex == -1)
                return (text, string.Empty);

            var before = separatorIndex > 0 ? text.Substring(0, separatorIndex) : string.Empty;
            var after = separatorIndex + 1 < text.Length ? text.Substring(separatorIndex + 1) : string.Empty;
            return (before.Trim(), after.Trim());
        }
    }
}
