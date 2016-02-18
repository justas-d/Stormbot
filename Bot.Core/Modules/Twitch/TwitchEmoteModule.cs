using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    public class TwitchEmoteModule : IModule
    {
        private const string EmotePrefix = ".";

        private readonly List<CentralizedEmoteSource> _emoteSources = new List<CentralizedEmoteSource>
        {
            new GlobalTwitchEmoteSource(),
            new BttvEmoteSource()
        };

        private DiscordClient _client;
        private HttpService _http;

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            Task.Run(async () =>
            {
                foreach (CentralizedEmoteSource source in _emoteSources)
                {
                    try
                    {
                        await source.FetchData(_http);
                    }
                    catch (Exception ex)
                    {
                        Logger.FormattedWrite(
                            GetType().Name,
                            $"Failed loading emotes for {source.GetType().Name}. Exception: {ex}");
                    }
                }
                GC.Collect();
            });

            manager.CreateDynCommands("emote", PermissionLevel.User, group =>
            {
                group.CreateCommand("")
                .Parameter("emote", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string emotePath = await ResolveEmoteDir(e.GetArg("emote"));

                        if (!File.Exists(emotePath))
                            return; // todo : lower case == upper case in this case. KAPPA = Kappa

                        await e.Channel.SafeSendFile(emotePath);
                    });
            });
        }

        private async Task<string> ResolveEmoteDir(string userInput)
        {
            foreach (CentralizedEmoteSource source in _emoteSources)
                if (source.ContainsEmote(userInput)) return await source.GetEmote(userInput, _http);

            return null;
        }
    }
}