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
    public class TwitchEmoteModule : IModule, IDataObject
    {
        private readonly List<EmoteSourceBase> _emoteSources = new List<EmoteSourceBase>
        {
            new GlobalTwitchEmoteSource(),
            new BttvEmoteSource()
        };

        [DataLoad, DataSave] private List<BttvChannelEmoteSource> _bttvChannelEmoteSources;
        private DiscordClient _client;
        private HttpService _http;

        void IDataObject.OnDataLoad()
        {
            if (_bttvChannelEmoteSources == null)
                _bttvChannelEmoteSources = new List<BttvChannelEmoteSource>();

            Task.Run(async () =>
            {
                for (int i = 0; i < _bttvChannelEmoteSources.Count; i++)
                {
                    _bttvChannelEmoteSources[i] = await BttvChannelEmoteSource.Create(_client,
                        _bttvChannelEmoteSources[i].Channel);
                }
            });
        }

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            Task.Run(async () =>
            {
                foreach (EmoteSourceBase source in _emoteSources)
                {
                    try
                    {
                        await source.DownloadData(_http);
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

            manager.CreateDynCommands("bttv", PermissionLevel.User, group =>
            {
                group.CreateCommand("")
                    .Parameter("channel")
                    .Description("Add a BTTV channel to the emote sources.")
                    .Do(async e =>
                    {
                        string channel = e.GetArg("channel");

                        if (_bttvChannelEmoteSources.FirstOrDefault(s => s.Channel == channel) != null)
                        {
                            await e.Channel.SendMessage("This channel is already in the emote source list.");
                            return;
                        }

                        BttvChannelEmoteSource source = await BttvChannelEmoteSource.Create(_client, channel);
                        if (source == null)
                        {
                            await e.Channel.SafeSendMessage("Failed getting emote data.");
                            return;
                        }

                        _bttvChannelEmoteSources.Add(source);
                        await e.Channel.SafeSendMessage($"Added channel {channel} to the emote source.");
                    });
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
            foreach (EmoteSourceBase source in _emoteSources)
                if (source.ContainsEmote(userInput)) return await source.GetEmote(userInput, _http);

            foreach (BttvChannelEmoteSource source in _bttvChannelEmoteSources)
                if (source.ContainsEmote(userInput)) return await source.GetEmote(userInput, _http);

            return null;
        }
    }
}