using System;

namespace TwitchIRCClient
{
    public class IrcMessageEventArgs : EventArgs
    {
        public IrcMessage Message { get; }

        public IrcMessageEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
