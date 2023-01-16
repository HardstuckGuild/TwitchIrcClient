using System;

namespace TwitchIrcClient
{
    /// <summary>
    /// Event Args for StateChange event.
    /// Immutable.
    /// </summary>
    [Obsolete("This wrapper class will be removed in future versions.")]
    public sealed class IrcChangedEventArgs : EventArgs
    {
        /// <summary>
        /// IrcStates of the new state currently triggered.
        /// </summary>
        public IrcState NewState { get; }

        /// <summary>
        /// In which channel did the new state occured in, if related.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Create a new event args based on the specified new IRC state.
        /// </summary>
        /// <param name="newState">new IRC state</param>
        [Obsolete("This wrapper class will be removed in future versions.")]
        public IrcChangedEventArgs(IrcState newState)
        {
            NewState = newState;
        }

        /// <summary>
        /// Create a new event args based on the specified new IRC state and specified channel.
        /// </summary>
        /// <param name="newState">new IRC state</param>
        /// <param name="channel">channel where new state occurred</param>
        [Obsolete("This wrapper class will be removed in future versions.")]
        public IrcChangedEventArgs(IrcState newState, string channel)
        {
            NewState = newState;
            Channel = channel;
        }
    }
}
