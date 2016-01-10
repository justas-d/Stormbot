// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;

namespace Stormbot.Bot.Core.Modules
{
    public class TwitchModule : IModule
    {
        private DiscordClient _client;
        private TwitchEmoteService _emotes;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _emotes = _client.Services.Get<TwitchEmoteService>();

            manager.CreateCommands(
                "twitch",
                group =>
                {
                    group.MinPermissions((int) PermissionLevel.User);

                    group.CreateCommand("randemote")
                        .Description("Sends a random twitch emote over chat.")
                        .Do(async e =>
                        {
                            await e.Channel.SendFile(await _emotes.GetRandomEmote());
                        });

                    group.CreateCommand("emote")
                        .Description("Displays a twitch emote")
                        .Parameter("emote")
                        .Do(async e =>
                        {
                            // todo : if we have more then one emotes given, combine them into one image and post it.
                            string emoteDir = await _emotes.ResolveEmoteDir(e.GetArg("emote"));
                            if (string.IsNullOrEmpty(emoteDir)) return;

                            await e.Channel.SendFile(emoteDir);
                        });
                });
        }
    }
}