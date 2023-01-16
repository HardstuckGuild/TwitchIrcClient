using System;

namespace TwitchIrcClient
{
    /// <summary>
    /// Event Args for ReceiveMessage event.
    /// Immutable.
    /// </summary>
    [Obsolete("This wrapper class will be removed in future versions.")]
    public sealed class IrcMessageEventArgs : EventArgs
    {
        /// <summary>
        /// The IRC message received.
        /// </summary>
        public IrcMessage Message { get; }

        /// <summary>
        /// Create an IRC Message Event Args with specified message.
        /// </summary>
        /// <param name="message">IRC message</param>
        [Obsolete("This wrapper class will be removed in future versions.")]
        public IrcMessageEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
