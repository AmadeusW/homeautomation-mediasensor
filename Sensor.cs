using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace MediaSensor
{
    /// <summary>
    /// Reports current media state and raises event when media state changes.
    /// </summary>
    internal class Sensor : IDisposable
    {
        /// <summary>
        /// Latched value of sensor state
        /// </summary>
        internal MediaState CurrentState { get; private set; }

        /// <summary>
        /// Fired when <see cref="CurrentState"/> changes
        /// </summary>
        internal event EventHandler<SensorStateEventArgs> StateChanged;

        /// <summary>
        /// Immediate value of sensor state
        /// </summary>
        private MediaState TransientState { get; set; }

        private Timer SensorTimer { get; set; }
        public Timer LatchTimer { get; private set; }
        private bool Initialized { get; set; }

        internal Sensor()
        {
        }

        internal void Initialize(ConfigurationReader configuration)
        {
            this.SensorTimer = new Timer(configuration.Poll);
            this.SensorTimer.Elapsed += OnPollingDelayElapsed;
            this.LatchTimer = new Timer(configuration.Latch);
            this.LatchTimer.AutoReset = false;
            this.LatchTimer.Elapsed += OnLatchingDelayElapsed;
            this.Initialized = true;
        }

        internal void Start()
        {
            if (this.Initialized)
            {
                this.SensorTimer.Start();
            }
        }

        internal void Stop()
        {
            if (this.Initialized)
            {
                this.SensorTimer.Stop();
                this.LatchTimer.Stop();
            }
        }

        public void Dispose()
        {
            SensorTimer.Stop();
            SensorTimer.Elapsed -= OnPollingDelayElapsed;
            LatchTimer.Stop();
            LatchTimer.Elapsed -= OnLatchingDelayElapsed;
        }

        private void OnPollingDelayElapsed(object sender, ElapsedEventArgs e)
        {
            var newState = SensorCore.GetState();
            if (newState != TransientState)
            {
                TransientState = newState;
                LatchTimer.Stop(); // Reset the latch timer
                LatchTimer.Start();
            }
        }

        private void OnLatchingDelayElapsed(object sender, ElapsedEventArgs e)
        {
            // If state has changed during latching delay, this timer would have been reset
            // and would not have elapsed.
            // If we're running this code, it means that the transient value has not changed.
            if (CurrentState != TransientState)
            {
                CurrentState = TransientState;
                StateChanged?.Invoke(this, new SensorStateEventArgs(CurrentState));
            }
            else
            {
                // We just averted a glitch
            }
        }
    }

    internal enum MediaState
    {
        Stopped,
        Standby,
        Playing
    }

    internal class SensorStateEventArgs : EventArgs
    {
        internal MediaState State { get; }

        internal SensorStateEventArgs(MediaState state)
        {
            this.State = state;
        }
    }

    /// <summary>
    /// Contains code which detects status of the media.
    /// Source: https://stackoverflow.com/a/45483843/879243
    /// Thank you, Simon Mourier
    /// </summary>
    internal static class SensorCore
    {
        public static MediaState GetState()
        {
            var enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            var speakers = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            var meter = (IAudioMeterInformation)speakers.Activate(typeof(IAudioMeterInformation).GUID, 0, IntPtr.Zero);
            var value = meter.GetPeakValue();

            // this is a bit tricky. 0 is the official "no sound" value
            // but for example, if you open a video and plays/stops with it (w/o killing the app/window/stream),
            // the value will not be zero, but something really small (around 1E-09)
            // so, depending on your context, it is up to you to decide
            // if you want to test for 0 or for a small value
            if (value == 0)
                return MediaState.Stopped;
            else if (value <= 1E-08)
                return MediaState.Standby;
            else
                return MediaState.Playing;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        private enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
        }

        private enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        private interface IMMDeviceEnumerator
        {
            void NotNeeded();
            IMMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role);
            // the rest is not defined/needed
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        private interface IMMDevice
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object Activate([MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr pActivationParams);
            // the rest is not defined/needed
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        private interface IAudioMeterInformation
        {
            float GetPeakValue();
            // the rest is not defined/needed
        }
    }
}
