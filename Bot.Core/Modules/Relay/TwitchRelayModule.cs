using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;
using TwitchBotBase.Twitch.Bot;

namespace Stormbot.Bot.Core.Modules.Relay
{
    public class TwitchRelayModule : IDataModule
    {
        private static readonly string EscapePrefix = "}";
        private const int MaxViewsPerChannel = 500;

        private DiscordClient _client;

        [DataLoad, DataSave]
        private Dictionary<string, List<ulong>> _serializeRelays;

        private readonly Dictionary<string, List<Channel>> _relays = new Dictionary<string, List<Channel>>();

        private TwitchBot _twitch;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _twitch = new TwitchBot();

            manager.CreateCommands("twitch", group =>
            {
                group.CreateCommand("connect")
                    .MinPermissions((int) PermissionLevel.ChannelModerator)
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
                                    e.Channel.SendMessage(
                                        $"{channel}'s view count ({viewers}) is currently over view barrier ({MaxViewsPerChannel}), therefore, for the sake of not getting a cooldown for spamming Discord, you cannot connect to this channel.");
                        }
                        else
                            await e.Channel.SendMessage($"{channel} channel is currently offline.");
                    });

                group.CreateCommand("disconnect")
                    .MinPermissions((int) PermissionLevel.ChannelModerator)
                    .Parameter("channel")
                    .Do(async e =>
                    {
                        await Unsubscribe(e.GetArg("channel"), e.Channel);
                    });
                group.CreateCommand("list")
                    .Do(async e =>
                    {
                        List<string> twitchSub = GetTwitchChannels(e.Channel).ToList();

                        if (!twitchSub.Any())
                        {
                            await e.Channel.SendMessage("This channel isin't subscribed to any twitch channels.");
                            return;
                        }

                        StringBuilder builder = new StringBuilder($"**{e.Channel.Name} is subscribed to:**\r\n```");
                        foreach (string twitchChannel in twitchSub)
                            builder.AppendLine($"* {twitchChannel}");

                        await e.Channel.SendMessage($"{builder.ToString()}```");
                    });
            });

            // connect the twitch bot to tmi.twitch.tc
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
                if (e.Message.Username == Constants.TwitchUsername) return;

                if (e.Message.Text.StartsWith(EscapePrefix)) return;

                foreach (Channel relay in _relays[e.Message.Channel])
                    await relay.SendMessage($"**Twitch**: `<{e.Message.Username}> {e.Message.Text}`");
            };

            _twitch.ChannelLeave += (s, e) =>
            {
                _relays.Remove(e.Channel);
                _serializeRelays.Remove(e.Channel);
            };

            _client.MessageReceived += (s, e) =>
            {
                if (e.Message.IsAuthor) return;
                if (e.Message.Text.StartsWith(EscapePrefix)) return;

                foreach (string twitchChannel in GetTwitchChannels(e.Channel))
                    _twitch.SendMessage(twitchChannel, $"{e.User.Name}@{e.Channel.Name}: {e.Message.Text}");
            };
        }

        private void TwitchTryConnect()
            => _twitch.Connect(Constants.TwitchUsername, Constants.TwitchOauth);

        /// <summary>
        /// Returns all the twitch channels the given discord channel is subscribed to.
        /// </summary>
        private IEnumerable<string> GetTwitchChannels(Channel discordChannel)
            => (from pair in _relays
                where pair.Value.FirstOrDefault(c => c.Id == discordChannel.Id) != null
                select pair.Key);

        private async Task Unsubscribe(string twitchChannel, Channel discordChannel)
        {
            // normalize channel name.
            twitchChannel = TwitchBot.NormalizeChannelName(twitchChannel);

            // dont try to remove from a list of subscribers in a twitch channel if we dont even have the channel in the relays list.
            if (!_relays.ContainsKey(twitchChannel)) return;

            // if the twitch relay discord connected channel list doesn't include the discord channel that
            // is passed as the argument, return. We don't want to try to delete something that doesn't exist.
            if (_relays[twitchChannel].FirstOrDefault(c => c.Id == discordChannel.Id) == null) return;

            // Remove all of the channels in the twitchChannel entry which have the same id as the
            // discord channel that we passed in the arguments
            _relays[twitchChannel].RemoveAll(match => match.Id == discordChannel.Id);
            _serializeRelays[twitchChannel].Remove(discordChannel.Id); // do the same in the serialization data.

            // we will command the twitch bot to disconnect from the twitch channel if no
            // discord channel is connected to it.
            if(!_relays[twitchChannel].Any())
                _twitch.PartChannel(twitchChannel);

            await discordChannel.SendMessage($"Unsubscribed from twitch chat: {twitchChannel}");
        }

        private async Task Subscribe(string twitchChannel, Channel discordChannel)
        {
            // normalize channel name.
            twitchChannel = TwitchBot.NormalizeChannelName(twitchChannel);

            // if the twitch bot is not in the twitch channel, tell it to join it.
            if(!_twitch.Channels.Contains(twitchChannel))
                _twitch.JoinChannel(twitchChannel); // todo : only join if the view count if less then ~500.

            // if there is no entry in _relays for the twitch channel, create one.
            if (!_relays.ContainsKey(twitchChannel))
            {
                List<Channel> channel = new List<Channel> {discordChannel};
                _relays.Add(twitchChannel, channel);
                _serializeRelays.Add(twitchChannel, channel.Select(d => d.Id).ToList()); // serialize data dict
                return;
            }

            // reference the list of channels found by the twitch channel key so we dont have to look it up every time.
            List<Channel> subRef = _relays[twitchChannel];

            // check if the discord channel is already subscribed to the twitch channel.
            if (subRef.FirstOrDefault(c => c.Id == discordChannel.Id) != null)
                return;

            // add the discord channel to the twitch channel subscribers list.
            subRef.Add(discordChannel);
            _serializeRelays[twitchChannel].Add(discordChannel.Id); // add it to the serialize data.

            await discordChannel.SendMessage($"Subscribed to twitch chat: {twitchChannel}");
        }

        #region TwitchApiWrap


        private async Task<dynamic> GetChannelData(string channel)
        {
            string callback = await Utils.AsyncDownloadRaw($"https://api.twitch.tv/kraken/streams/{channel}");

            return string.IsNullOrEmpty(callback) ? null : JObject.Parse(callback);
        }

        /// <summary>
        /// Returns whether the given channel name is a valid twitch channel and that it is currently streaming.
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

        public void OnDataLoad()
        {
            // if we did not have any previous data on disk, create a new _serializeRelays so we dont get null ref ex.
            if (_serializeRelays == null)
            {
                _serializeRelays = new Dictionary<string, List<ulong>>();
                return;
            }

            // load the data from _serializeRelays into _relays using Subscribe()
            Task.Run(async () =>
            {
                foreach (var pair in _serializeRelays)
                {
                    foreach (ulong channel in pair.Value)
                        await Subscribe(pair.Key, _client.GetChannel(channel));
                }
            });
        }
    }
}
