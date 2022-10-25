using System;

namespace TwitchIRCClient
{
    /// <summary>
    /// Event Args for ReceiveMessage event.
    /// Immutable.
    /// </summary>
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
        public IrcMessageEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
