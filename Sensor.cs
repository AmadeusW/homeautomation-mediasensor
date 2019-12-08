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
        internal MediaState CurrentState { get; private set; }
        internal event EventHandler<SensorStateEventArgs> StateChanged;
        private Timer Timer { get; }

        internal Sensor()
        {
            this.Timer = new Timer(500);
            this.Timer.Elapsed += OnTimerElapsed;
        }

        internal void Start()
        {
            this.Timer.Start();
        }

        internal void Stop()
        {
            this.Timer.Stop();
        }

        public void Dispose()
        {
            Timer.Stop();
            Timer.Elapsed -= OnTimerElapsed;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var newState = SensorCore.GetState();
            if (newState != CurrentState)
            {
                CurrentState = newState;
                StateChanged?.Invoke(this, new SensorStateEventArgs(CurrentState));
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
