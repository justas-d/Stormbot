using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    public class TwitchEmoteService : IDataModule
    {
        private HttpService _http;
        private DiscordClient _client;

        private const string EmotePrefix = ".";

        private readonly List<CentralizedEmoteSource> _emoteSources = new List<CentralizedEmoteSource>
        {
            new GlobalTwitchEmoteSource(),
            new BttvEmoteSource(),
        };

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _http = _client.Services.Get<HttpService>();

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

            _client.MessageReceived += async (sender, args) =>
            {
                if (!manager.EnabledServers.Contains(args.Server) && !manager.EnabledChannels.Contains(args.Channel))
                    return;

                if (!args.Message.Text.StartsWith(EmotePrefix)) return;
                string emote = (args.Message.Text.Split(' ').FirstOrDefault()).Remove(0, 1);

                string emotePath = await ResolveEmoteDir(emote);
                if (!File.Exists(emotePath)) return; // todo : lower case == upper case in this case. KAPPA = Kappa

                await args.Channel.SendFile(emotePath);
            };
        }

        private async Task<string> ResolveEmoteDir(string userInput)
        {
            foreach (CentralizedEmoteSource source in _emoteSources)
                if (source.ContainsEmote(userInput)) return await source.GetEmote(userInput, _http);

            return null;
        }

        void IDataModule.OnDataLoad() { }
    }
}
