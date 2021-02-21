using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchIRCClient
{
    public class TwitchIrcClient : IDisposable
    {
        // public
        /// <summary>
        /// Last joined channel.
        /// </summary>
        public string LastChannelName { get; private set; } = "";

        /// <summary>
        /// A copy of a list of all channels currently joined.
        /// </summary>
        public List<string> ChannelNames
        {
            get
            {
                return new List<string>(_channelNames);
            }
        }

        /// <summary>
        /// Indicates whether the connection is about to commence.
        /// </summary>
        public bool Connecting { get; private set; } = false;

        /// <summary>
        /// Indicates whether a successfull connection with Twitch has been estabilished.
        /// </summary>
        public bool Connected { get; private set; } = false;

        /// <summary>
        /// A thread for message invokes.
        /// </summary>
        public Thread ReadMessagesThread { get; private set; }

        /// <summary>
        /// An event that triggers on new IRC messages being received.
        /// </summary>
        public event EventHandler<IrcMessageEventArgs> ReceiveMessage;

        /// <summary>
        /// An event that triggers on new IRC state change.
        /// </summary>
        public event EventHandler<IrcChangedEventArgs> StateChange;

        // private
        private readonly List<string> _channelNames = new List<string>();
        private readonly string userName;
        private readonly string oauthPassword;
        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;

        // constants
        private const string serverIp = "irc.chat.twitch.tv";
        private const int serverPort = 6667;

        /// <summary>
        /// Create TwitchIrcClient class with specified user name and an oauth2 password.
        /// </summary>
        /// <param name="userName">Username for Twitch IRC</param>
        /// <param name="oauthPassword">OAuth2 password for Twitch IRC</param>
        public TwitchIrcClient(string userName, string oauthPassword)
        {
            this.userName = userName;
            this.oauthPassword = oauthPassword;
        }

        /// <summary>
        /// Create TwitchIrcClient class with specified user name, an oauth2 password and an immediate channel join.
        /// </summary>
        /// <param name="userName">Username for Twitch IRC</param>
        /// <param name="oauthPassword">OAuth2 password for Twitch IRC</param>
        /// <param name="channelName">Channel to connect to at startup</param>
        public TwitchIrcClient(string userName, string oauthPassword, string channelName)
        {
            this.userName = userName;
            this.oauthPassword = oauthPassword;
            LastChannelName = channelName.ToLower();
            _channelNames.Add(LastChannelName);
        }

        /// <summary>
        /// Start the connection to the IRC.
        /// </summary>
        public void BeginConnection()
        {
            try
            {
                SetupStreams();
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Connecting));
                _ = LoginAsync();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
            }
        }

        /// <summary>
        /// Start the connection to the IRC in asynchronous context.
        /// </summary>
        public async Task BeginConnectionAsync()
        {
            try
            {
                SetupStreams();
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Connecting));
                await LoginAsync();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
            }
        }

        /// <summary>
        /// Enter IRC room asynchronously.
        /// </summary>
        /// <param name="channelName">Room name to enter</param>
        /// <param name="partPreviousChannels">if set to true: leave all previous entered channels</param>
        /// <returns>A Task whether the room was entered successfully.</returns>
        public async Task<bool> JoinRoomAsync(string channelName, bool partPreviousChannels = false)
        {
            channelName = channelName.ToLower();
            if (_channelNames.Contains(channelName))
            {
                return false;
            }
            if (partPreviousChannels)
            {
                foreach (string name in _channelNames)
                {
                    await LeaveRoomAsync(name);
                }
                _channelNames.Clear();
            }
            try
            {
                await outputStream.WriteLineAsync($"JOIN #{channelName}");
                await outputStream.FlushAsync();
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.ChannelJoining, channelName));
                LastChannelName = channelName;
                _channelNames.Add(channelName);
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                return false;
            }
        }

        /// <summary>
        /// Leave IRC room asynchronously.
        /// </summary>
        /// <param name="channelName">Name of the room to leave</param>
        /// <returns>A Task whether the room was left successfully.</returns>
        public async Task<bool> LeaveRoomAsync(string channelName)
        {
            channelName = channelName.ToLower();
            if (!_channelNames.Contains(channelName))
            {
                return false;
            }
            try
            {
                await outputStream.WriteLineAsync($"PART #{channelName}");
                await outputStream.FlushAsync();
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.ChannelLeaving, channelName));
                _channelNames.Remove(channelName);
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                return false;
            }
        }

        /// <summary>
        /// Sends an IRC message to the server.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <returns>A Task whether the message was successfully sent.</returns>
        public async Task<bool> SendIrcMessageAsync(string message)
        {
            try
            {
                await outputStream.WriteLineAsync(message);
                await outputStream.FlushAsync();
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                return false;
            }
        }

        /// <summary>
        /// Sends a Twitch message to a specified channel.
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <param name="message">Message to send</param>
        /// <returns>A Task whether the message was successfully sent.</returns>
        public async Task<bool> SendChatMessageAsync(string channelName, string message)
        {
            channelName = channelName.ToLower();
            if (!_channelNames.Contains(channelName))
            {
                return false;
            }
            try
            {
                await outputStream.WriteLineAsync($":{userName}!{userName}@{userName}.tmi.twitch.tv PRIVMSG #{channelName} :{message}");
                await outputStream.FlushAsync();
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                return false;
            }
        }

        /// <summary>
        /// Disposes of the resources held by the class.
        /// </summary>
        public void Dispose()
        {
            Connecting = false;
            Connected = false;
            ReadMessagesThread?.Abort();
            inputStream?.Dispose();
            outputStream?.Dispose();
            tcpClient?.Close();
        }

        private void SetupStreams()
        {
            tcpClient = new TcpClient(serverIp, serverPort);
            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());
        }

        private async Task LoginAsync()
        {
            try
            {
                await outputStream.WriteLineAsync($"PASS {oauthPassword}");
                await outputStream.WriteLineAsync($"NICK {userName}");
                if (LastChannelName != "")
                {
                    await outputStream.WriteLineAsync($"JOIN #{LastChannelName}");
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.ChannelJoining, LastChannelName));
                }
                await outputStream.FlushAsync();
                Connecting = true;
                ReceiveMessage += OnMessageReceived;
                ReadMessagesThread = new Thread(ReadMessagesAsync)
                {
                    IsBackground = true
                };
                ReadMessagesThread.Start();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
            }
        }

        private async void ReadMessagesAsync()
        {
            while (Connecting || Connected)
            {
                try
                {
                    string message = await inputStream.ReadLineAsync();
                    ReceiveMessage?.Invoke(this, new IrcMessageEventArgs(new IrcMessage(message)));
                }
                catch
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                    Connected = false;
                    Connecting = false;
                }
            }
        }

        protected async void OnMessageReceived(object sender, IrcMessageEventArgs e)
        {
            if ((e == null) || (e.Message == null))
            {
                if (Connecting && !Connected)
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.FailedConnection));
                }
                else
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Disconnected));
                }
                Connecting = false;
                Connected = false;
                return;
            }
            if (!Connected && e.Message.OriginalMessage.Equals($":tmi.twitch.tv 001 {userName} :Welcome, GLHF!"))
            {
                Connected = true;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.Connected));
            }
            else if (Connected && e.Message.OriginalMessage.Equals("PING :tmi.twitch.tv"))
            {
                await SendIrcMessageAsync("PONG :tmi.twitch.tv");
            }
            else if (Connected && e.Message.OriginalMessage.Equals($":{userName}.tmi.twitch.tv 353 {userName} = #{LastChannelName} :{userName}"))
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcStates.ChannelJoined, LastChannelName));
            }
        }
    }
}
