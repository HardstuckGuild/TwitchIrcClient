using System.Text.RegularExpressions;

namespace TwitchIRCClient
{
    public class IrcMessage
    {
        public string OriginalMessage { get; }

        public bool IsChannelMessage { get; } = false;

        public string ChannelName { get; }

        public string UserName { get; }

        public string ChannelMessage { get; }

        private IrcMessage(string message)
        {
            OriginalMessage = message;
            var channelRegex = new Regex(" PRIVMSG #.+ :", RegexOptions.CultureInvariant);
            IsChannelMessage = channelRegex.IsMatch(OriginalMessage);
            if (IsChannelMessage)
            {
                try
                {
                    var channelNameRegexString = channelRegex.Match(OriginalMessage);
                    var channelNameRegex = new Regex("#.+(?= )", RegexOptions.CultureInvariant);
                    var channelName = channelNameRegex.Match(channelNameRegexString.Value).Value.Substring(1);
                    ChannelName = channelName;

                    var userNameRegex = new Regex(":.+!", RegexOptions.CultureInvariant);
                    var dirtyUserName = userNameRegex.Match(OriginalMessage).Value.Substring(1);
                    var userName = dirtyUserName.Substring(0, dirtyUserName.Length - 1);
                    UserName = userName;

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

        internal static IrcMessage ParseMessage(string message) => new IrcMessage(message);
    }
}
