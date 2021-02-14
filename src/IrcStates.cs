namespace TwitchIRCClient
{
    public enum IrcStates
    {
        FailedConnection,
        Disconnected,
        Connecting,
        Connected,
        ChannelJoining,
        ChannelJoined,
        ChannelLeaving,
        ChannelLeft
    }
}
