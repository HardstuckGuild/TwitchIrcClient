using System.Text.RegularExpressions;

namespace TwitchIRCClient
{
    /// <summary>
    /// Represents a message received from IRC client.
    /// </summary>
    public sealed class IrcMessage
    {
        private static readonly Regex messageRegex = new Regex("#.+(?= )");
        private static readonly Regex channelRegex = new Regex(" PRIVMSG #.+ :");
        private static readonly Regex userNameRegex = new Regex(":.+!");

        /// <summary>
        /// The original, unmodified IRC message.
        /// </summary>
        public string OriginalMessage { get; }

        /// <summary>
        /// Indicates, whether the message is a Twitch chat message in a Twitch channel.
        /// </summary>
        public bool IsChannelMessage { get; }

        /// <summary>
        /// Name of the channel the Twitch chat message occured in, only if IsChannelMessage is true.
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// Name of the user who sent the Twitch chat message, only if IsChannelMessage is true.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The content of the Twitch chat message, only if IsChannelMessage is true.
        /// </summary>
        public string ChannelMessage { get; }

        /// <summary>
        /// Create an IRC message object with specified IRC message.
        /// </summary>
        /// <param name="message">an IRC message</param>
        public IrcMessage(string message)
        {
            OriginalMessage = message;
            IsChannelMessage = channelRegex.IsMatch(OriginalMessage);
            if (IsChannelMessage)
            {
                try
                {
                    var channelNameRegexString = channelRegex.Match(OriginalMessage);
                    ChannelName = messageRegex.Match(channelNameRegexString.Value).Value.Substring(1);

                    var dirtyUserName = userNameRegex.Match(OriginalMessage).Value.Substring(1);
                    UserName = dirtyUserName.Substring(0, dirtyUserName.Length - 1);

                    var messageSplit = channelRegex.Split(OriginalMessage);
                    ChannelMessage = messageSplit[1];
                }
                catch // something went wrong during splitting, message setting is most likely not correct
                {
                    IsChannelMessage = false;
                    ChannelName = null;
                    UserName = null;
                    ChannelMessage = null;
                }
            }
        }
    }
}
