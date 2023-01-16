using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchIrcClient
{
    /// <summary>
    /// A client for Twitch IRC.
    /// </summary>
    public sealed class TwitchChatClient : IDisposable
    {
        // public
        /// <summary>
        /// Last joined channel.
        /// </summary>
        public string LastChannelName { get; private set; } = "";

        /// <summary>
        /// A copy of a list of all channels currently joined.
        /// </summary>
        public List<string> ChannelNames => new(_channelNames);

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
        public event EventHandler<IrcMessage> ReceivedMessage;

        /// <summary>
        /// A wrapper handler around IrcState change and a specified IrcChannel name.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="newState">New IRC state</param>
        /// <param name="channelName">Channel name where the new state occurred</param>
        public delegate void StateChangeHandler(object sender, IrcState newState, string channelName = null);

        /// <summary>
        /// An event that triggers on new IRC state change.
        /// </summary>
        public event StateChangeHandler StateChanged;

        /// <summary>
        /// An event that triggers on new IRC messages being received.
        /// </summary>
        [Obsolete("This event is going to be removed soon in favour of ReceivedMessage event which does not use a wrapper class.")]
        public event EventHandler<IrcMessageEventArgs> ReceiveMessage;

        /// <summary>
        /// An event that triggers on new IRC state change.
        /// </summary>
        [Obsolete("This event is going to be removed soon in favour of StateChanged event which does not use a wrapper class.")]
        public event EventHandler<IrcChangedEventArgs> StateChange;

        // private
        private readonly List<string> _channelNames = new();
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
        public TwitchChatClient(string userName, string oauthPassword)
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
        public TwitchChatClient(string userName, string oauthPassword, string channelName)
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Connecting));
                StateChanged?.Invoke(this, IrcState.Connecting);
                _ = LoginAsync();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Connecting));
                StateChanged?.Invoke(this, IrcState.Connecting);
                await LoginAsync();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
            }
        }

        /// <summary>
        /// Enter IRC room asynchronously.
        /// </summary>
        /// <param name="channelName">Room name to enter</param>
        /// <param name="partPreviousChannels">If set to true: leave all previous entered channels</param>
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.ChannelJoining, channelName));
                StateChanged?.Invoke(this, IrcState.ChannelJoining, channelName);
                LastChannelName = channelName;
                _channelNames.Add(channelName);
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.ChannelLeaving, channelName));
                StateChanged?.Invoke(this, IrcState.ChannelLeaving, channelName);
                _channelNames.Remove(channelName);
                return true;
            }
            catch
            {
                Connected = false;
                Connecting = false;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
                return false;
            }
        }

        /// <summary>
        /// Sends an IRC message to the server asynchronously.
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
                return false;
            }
        }

        /// <summary>
        /// Sends a Twitch message to a specified channel asynchronously.
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
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
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
            inputStream?.Dispose();
            outputStream?.Dispose();
            tcpClient?.Close();
            ReadMessagesThread?.Join();
            GC.SuppressFinalize(this);
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
                if (LastChannelName != string.Empty)
                {
                    await outputStream.WriteLineAsync($"JOIN #{LastChannelName}");
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.ChannelJoining, LastChannelName));
                    StateChanged?.Invoke(this, IrcState.ChannelJoining, LastChannelName);
                }
                await outputStream.FlushAsync();
                Connecting = true;
                ReceiveMessage += OnMessageEventArgsReceived;
                ReceivedMessage += OnMessageReceived;
                ReadMessagesThread = new Thread(ReadMessagesAsync)
                {
                    IsBackground = true
                };
                ReadMessagesThread.Start();
            }
            catch
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                StateChanged?.Invoke(this, IrcState.Disconnected);
            }
        }

        private async void ReadMessagesAsync()
        {
            while (Connecting || Connected)
            {
                try
                {
                    var message = await inputStream.ReadLineAsync();
                    ReceiveMessage?.Invoke(this, new IrcMessageEventArgs(new IrcMessage(message)));
                    ReceivedMessage?.Invoke(this, new IrcMessage(message));
                }
                catch
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                    StateChanged?.Invoke(this, IrcState.Disconnected);
                    Connected = false;
                    Connecting = false;
                }
            }
        }

        private async void OnMessageEventArgsReceived(object sender, IrcMessageEventArgs e)
        {
            await ProcessMessageAsync(e?.Message);
        }

        private async void OnMessageReceived(object sender, IrcMessage message)
        {
            await ProcessMessageAsync(message);
        }

        private async Task ProcessMessageAsync(IrcMessage message)
        {
            if (message is null)
            {
                if (Connecting && !Connected)
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.FailedConnection));
                    StateChanged?.Invoke(this, IrcState.FailedConnection);
                }
                else
                {
                    StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Disconnected));
                    StateChanged?.Invoke(this, IrcState.Disconnected);
                }
                Connecting = false;
                Connected = false;
                return;
            }
            if (!Connected && message.OriginalMessage.Equals($":tmi.twitch.tv 001 {userName} :Welcome, GLHF!"))
            {
                Connected = true;
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.Connected));
                StateChanged?.Invoke(this, IrcState.Connected);
                return;
            }
            if (Connected && message.OriginalMessage.Equals("PING :tmi.twitch.tv"))
            {
                await SendIrcMessageAsync("PONG :tmi.twitch.tv");
                return;
            }
            if (Connected && message.OriginalMessage.Equals($":{userName}.tmi.twitch.tv 353 {userName} = #{LastChannelName} :{userName}"))
            {
                StateChange?.Invoke(this, new IrcChangedEventArgs(IrcState.ChannelJoined, LastChannelName));
                StateChanged?.Invoke(this, IrcState.ChannelJoined, LastChannelName);
                return;
            }
        }
    }
}
