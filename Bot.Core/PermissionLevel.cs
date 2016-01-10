namespace Stormbot.Bot.Core
{
    public enum PermissionLevel : byte
    {
        User = 0,
        Trusted,
        ChannelModerator,
        ChannelAdmin,
        ServerModerator,
        ServerAdmin,
        ServerOwner,
        BotOwner,
    }
}
