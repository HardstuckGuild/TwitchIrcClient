﻿using System;

namespace TwitchIRCClient
{
    public class IrcMessageEventArgs : EventArgs
    {
        public string Message { get; }

        public IrcMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
