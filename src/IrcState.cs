namespace TwitchIrcClient
{
    /// <summary>
    /// IrcStates enums, used within StateChange event.
    /// </summary>
    public enum IrcState
    {
        /// <summary>
        /// Indicates, that the connection to Twitch has failed.
        /// </summary>
        FailedConnection,

        /// <summary>
        /// Indicates, that a disconnection from Twitch has occured.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Indicates, that an attempt to connect to Twitch has been made.
        /// </summary>
        Connecting,

        /// <summary>
        /// Indicates, that a connection to Twitch has been successfully established.
        /// </summary>
        Connected,

        /// <summary>
        /// Indicates, that a request to join a channel has been fired.
        /// </summary>
        ChannelJoining,

        /// <summary>
        /// Indicates, that a channel was successfully joined.
        /// </summary>
        ChannelJoined,

        /// <summary>
        /// Indicates, that a request to leave a channel has been fired.
        /// </summary>
        ChannelLeaving,

        /// <summary>
        /// Indicates, that a channel was successfully left.
        /// </summary>
        ChannelLeft,
    }
}
