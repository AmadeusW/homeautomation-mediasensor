using System;

namespace MediaSensor
{
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

    internal enum TargetState
    {
        /// <summary>
        /// <see cref="MediaState"/> determines overall state
        /// </summary>
        FromMedia,

        /// <summary>
        /// State is on
        /// </summary>
        On,

        /// <summary>
        /// State is off
        /// </summary>
        Off,

        /// <summary>
        /// State is temporarily on, and will turn itself off
        /// </summary>
        Shutdown,
    }

    internal class OverrideStateArgs : EventArgs
    {
        internal TargetState State { get; }

        internal OverrideStateArgs(TargetState state)
        {
            this.State = state;
        }
    }

    internal class UpdateArgs : EventArgs
    {
        internal MediaState MediaState { get; }
        internal TargetState OverrideState { get; }
        internal Exception? Exception { get; }

        internal UpdateArgs(MediaState mediaState, TargetState overrideState, Exception? exception = null)
        {
            this.MediaState = mediaState;
            this.OverrideState = overrideState;
            this.Exception = exception;
        }
    }
}
