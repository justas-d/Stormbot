// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

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
