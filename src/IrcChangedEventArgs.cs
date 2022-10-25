using System;

namespace TwitchIRCClient
{
    /// <summary>
    /// Event Args for StateChange event.
    /// Immutable.
    /// </summary>
    public sealed class IrcChangedEventArgs : EventArgs
    {
        /// <summary>
        /// IrcStates of the new state currently triggered.
        /// </summary>
        public IrcStates NewState { get; }

        /// <summary>
        /// In which channel did the new state occured in, if related.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Create a new event args based on the specified new IRC state.
        /// </summary>
        /// <param name="newState">new IRC state</param>
        public IrcChangedEventArgs(IrcStates newState)
        {
            NewState = newState;
        }

        /// <summary>
        /// Create a new event args based on the specified new IRC state and specified channel.
        /// </summary>
        /// <param name="newState">new IRC state</param>
        /// <param name="channel">channel where new state occured</param>
        public IrcChangedEventArgs(IrcStates newState, string channel)
        {
            NewState = newState;
            Channel = channel;
        }
    }
}
