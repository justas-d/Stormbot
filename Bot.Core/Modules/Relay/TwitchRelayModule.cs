using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;
using TwitchBotBase.Twitch.Bot;

namespace Stormbot.Bot.Core.Modules.Relay
{
    public class TwitchRelayModule : IDataObject, IModule
    {
        private const int MaxViewsPerChannel = 500;
        private static readonly string EscapePrefix = "}";
        private DiscordClient _client;

        [DataLoad, DataSave] private Dictionary<string, List<JsonChannel>> _relays =
            new Dictionary<string, List<JsonChannel>>();

        private TwitchBot _twitch;

        void IDataObject.OnDataLoad()
        {
            if (_relays == null)
            {
                _relays = new Dictionary<string, List<JsonChannel>>();
                return;
            }

            // load the data from _serializeRelays into _relays using Subscribe()
            Task.Run(async () =>
            {
                foreach (var pair in _relays)
                {
                    List<JsonChannel> removeChannels = new List<JsonChannel>();

                    foreach (JsonChannel channel in pair.Value)
                    {
                        if (channel.FinishLoading(_client))
                            await Subscribe(pair.Key, channel);
                        else
                        {
                            Logger.FormattedWrite("TwitchRelayLoad",
                                $"Tried to load TwitchRelay discord channel for nonexistant channel id : {channel.ChannelId}. Removing",
                                ConsoleColor.Yellow);
                            removeChannels.Add(channel);
                        }
                    }

                    foreach (JsonChannel channel in removeChannels)
                        pair.Value.Remove(channel);

                    if (!pair.Value.Any())
                        _relays.Remove(pair.Key);
                }
            });
        }

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;
            _twitch = new TwitchBot();

            manager.CreateDynCommands("twitch", PermissionLevel.ChannelModerator, group =>
            {
                group.CreateCommand("connect")
                    .Description("Connects this channel to a given twitch channel, relaying the messages between them.")
                    .Parameter("channel")
                    .Do(async e =>
                    {
                        string channel = e.GetArg("channel");

                        if (await IsValidTwitchChannelAndIsStreaming(channel))
                        {
                            int? viewers = await GetViewerCount(channel);

                            if (viewers != null && viewers.Value <= MaxViewsPerChannel)
                                await Subscribe(channel, e.Channel);
                            else
                                await
                                    e.Channel.SafeSendMessage(
                                        $"{channel}'s view count ({viewers}) is currently over the view barrier ({MaxViewsPerChannel}), therefore, for the sake of not getting a cooldown for spamming Discord, we cannot connect to this channel.");
                        }
                        else
                            await e.Channel.SafeSendMessage($"{channel} channel is currently offline.");
                    });

                group.CreateCommand("disconnect")
                    .Description("Disconnects this channel from the given twitch channel.")
                    .Parameter("channel")
                    .Do(async e => { await Unsubscribe(e.GetArg("channel"), e.Channel); });
                group.CreateCommand("list")
                    .MinDynPermissions((int) PermissionLevel.User)
                    .Description("Lists all the twitch channels this discord channel is connected to.")
                    .Do(async e =>
                    {
                        List<string> twitchSub = GetTwitchChannels(e.Channel).ToList();

                        if (!twitchSub.Any())
                        {
                            await e.Channel.SafeSendMessage("This channel isin't subscribed to any twitch channels.");
                            return;
                        }

                        StringBuilder builder = new StringBuilder($"**{e.Channel.Name} is subscribed to:**\r\n```");
                        foreach (string twitchChannel in twitchSub)
                            builder.AppendLine($"* {twitchChannel}");

                        await e.Channel.SafeSendMessage($"{builder.ToString()}```");
                    });
            });

            // connect the twitch bot to tmi.twitch.tv
            TwitchTryConnect();

            _twitch.DisconnectFromTwitch +=
                async (s, e) =>
                {
                    Logger.FormattedWrite("Twitch", "Disconnected from twitch", ConsoleColor.Red);

                    // try to reconnect to twitch
                    while (!_twitch.IsConnected)
                    {
                        Logger.FormattedWrite("Twitch", "Attempting to reconnect to twitch...", ConsoleColor.Red);
                        TwitchTryConnect();
                        await Task.Delay(5000);
                    }

                    Logger.FormattedWrite("Twitch", "Reconnected to twitch.", ConsoleColor.Red);
                };

            _twitch.ChatMessageReceived += async (s, e) =>
            {
                if (!_relays.ContainsKey(e.Message.Channel)) return;
                if (e.Message.Username == Config.TwitchUsername) return;

                if (e.Message.Text.StartsWith(EscapePrefix)) return;

                foreach (JsonChannel relay in _relays[e.Message.Channel])
                    await relay.Channel.SafeSendMessage($"**Twitch**: `<{e.Message.Username}> {e.Message.Text}`");
            };

            _twitch.ChannelLeave += (s, e) => _relays.Remove(e.Channel);

            _client.MessageReceived += (s, e) =>
            {
                if (e.Message.IsAuthor) return;
                if (e.Message.Text.StartsWith(EscapePrefix)) return;

                foreach (string twitchChannel in GetTwitchChannels(e.Channel))
                    _twitch.SendMessage(twitchChannel, $"{e.User.Name}@{e.Channel.Name}: {e.Message.Text}");
            };
        }

        private void TwitchTryConnect()
            => _twitch.Connect(Config.TwitchUsername, Config.TwitchOauth);

        /// <summary>
        ///     Returns all the twitch channels the given discord channel is subscribed to.
        /// </summary>
        private IEnumerable<string> GetTwitchChannels(Channel discordChannel)
            => (from pair in _relays
                where pair.Value.FirstOrDefault(c => c.Channel.Id == discordChannel.Id) != null
                select pair.Key);

        private async Task Unsubscribe(string twitchChannel, Channel discordChannel)
        {
            // normalize channel name.
            twitchChannel = TwitchBot.NormalizeChannelName(twitchChannel);

            // dont try to remove from a list of subscribers in a twitch channel if we dont even have the channel in the relays list.
            if (!_relays.ContainsKey(twitchChannel)) return;

            // if the twitch relay discord connected channel list doesn't include the discord channel that
            // is passed as the argument, return. We don't want to try to delete something that doesn't exist.
            if (_relays[twitchChannel].FirstOrDefault(c => c.Channel.Id == discordChannel.Id) == null) return;

            // Remove all of the channels in the twitchChannel entry which have the same id as the
            // discord channel that we passed in the arguments
            _relays[twitchChannel].RemoveAll(match => match.Channel.Id == discordChannel.Id);

            // we will command the twitch bot to disconnect from the twitch channel if no
            // discord channel is connected to it.
            if (!_relays[twitchChannel].Any())
                _twitch.PartChannel(twitchChannel);

            await discordChannel.SafeSendMessage($"Unsubscribed from twitch chat: {twitchChannel}");
        }

        private async Task Subscribe(string twitchChannel, Channel discordChannel)
        {
            // normalize channel name.
            twitchChannel = TwitchBot.NormalizeChannelName(twitchChannel);

            // if the twitch bot is not in the twitch channel, tell it to join it.
            if (!_twitch.Channels.Contains(twitchChannel))
                _twitch.JoinChannel(twitchChannel);

            // if there is no entry in _relays for the twitch channel, create one.
            if (!_relays.ContainsKey(twitchChannel))
            {
                List<JsonChannel> channel = new List<JsonChannel> {discordChannel};
                _relays.Add(twitchChannel, channel);
                return;
            }

            // reference the list of channels found by the twitch channel key so we dont have to look it up every time.
            List<JsonChannel> subRef = _relays[twitchChannel];

            // check if the discord channel is already subscribed to the twitch channel.
            if (subRef.FirstOrDefault(c => c.Channel.Id == discordChannel.Id) != null)
                return;

            // add the discord channel to the twitch channel subscribers list.
            subRef.Add(discordChannel);

            await discordChannel.SafeSendMessage($"Subscribed to twitch chat: {twitchChannel}");
        }

        #region TwitchApiWrap

        private async Task<dynamic> GetChannelData(string channel)
        {
            string callback = await Utils.AsyncDownloadRaw($"https://api.twitch.tv/kraken/streams/{channel}");
            return string.IsNullOrEmpty(callback) ? null : JObject.Parse(callback);
        }

        /// <summary>
        ///     Returns whether the given channel name is a valid twitch channel and that it is currently streaming.
        /// </summary>
        private async Task<bool> IsValidTwitchChannelAndIsStreaming(string channel)
        {
            dynamic channelData = await GetChannelData(channel);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse; it really isin't resharper please.
            if (ReferenceEquals(null, channelData)) return false;

            if (channelData["error"] != null)
                return false;

            if (channelData["stream"] == null)
                return false;

            return true;
        }

        private async Task<int?> GetViewerCount(string channel)
        {
            try
            {
                return (int) (await GetChannelData(channel)).stream.viewers;
            }
            catch
            {
            } // ignored

            return null;
        }

        #endregion
    }
}