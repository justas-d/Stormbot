using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;

namespace Stormbot.Bot.Core.Modules
{
    public class AnnouncementModule : IDataModule
    {
        [JsonObject(MemberSerialization.OptIn)]
        private class UserEventCallback
        {
            private Channel _channel;

            public Channel Channel
            {
                get { return _channel; }
                set
                {
                    _channel = value;
                    ChannelId = value.Id;
                }
            }

            [JsonProperty]
            public string Message { get; set; }

            [JsonProperty]
            public bool IsEnabled { get; set; }

            [JsonProperty]
            public ulong ChannelId { get; private set; }

            [JsonConstructor]
            private UserEventCallback(ulong channelid, string message, bool isenabled)
            {
                ChannelId = channelid;
                Message = message;
                IsEnabled = isenabled;
            }

            public UserEventCallback(Channel channel, string message) : this(channel.Id, message, true)
            {
                Channel = channel;
            }
        }

        [DataSave, DataLoad] private ConcurrentDictionary<ulong, UserEventCallback> _userJoinedSubs;
        [DataSave, DataLoad] private ConcurrentDictionary<ulong, UserEventCallback> _userLeftSubs;

        private const string UserNameKeyword = "|userName|";
        private const string LocationKeyword = "|location|";

        private static readonly string DefaultMessage = $"{UserNameKeyword} has joined {LocationKeyword}!";

        private string SyntaxMessage =>
            $"**Syntax:**\r\n```" +
            $"- {UserNameKeyword} - replaced with the name of the user who triggered the event" +
            $"- {LocationKeyword} - replaced with the location (server or channel) where the event occured.```";

        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("announce", group =>
            {
                group.CreateGroup("join", joinGroup =>
                {
                    // joinGroup callback exists commands
                    joinGroup.CreateGroup("", existsJoin =>
                    {
                        existsJoin.AddCheck((cmd, usr, chnl) =>
                        {
                            if (_userJoinedSubs.ContainsKey(chnl.Server.Id))
                                return _userJoinedSubs[chnl.Server.Id].IsEnabled;

                            return false;
                        });

                        existsJoin.CreateCommand("message")
                            .Description($"Sets the join message for this current server.\r\n{SyntaxMessage}")
                            .Parameter("message", ParameterType.Unparsed)
                            .Do(async e =>
                            {
                                string msg = e.GetArg("message");
                                _userJoinedSubs[e.Server.Id].Message = msg;
                                await e.Channel.SendMessage($"Set join message to {msg}");
                            });
                        existsJoin.CreateCommand("channel")
                            .Description("Sets the callback channel for this servers join announcements.")
                            .Parameter("channelName", ParameterType.Unparsed)
                            .Do(async e =>
                            {
                                string channelName = e.GetArg("channelName").ToLowerInvariant();
                                Channel channel =
                                    e.Server.TextChannels.FirstOrDefault(c => c.Name.ToLowerInvariant() == channelName);

                                if (channel == null)
                                {
                                    await e.Channel.SendMessage($"Channel with the name {channelName} was not found.");
                                    return;
                                }

                                _userJoinedSubs[e.Server.Id].Channel = channel;
                                await e.Channel.SendMessage($"Set join callback to channel {channel.Name}");
                            });
                        existsJoin.CreateCommand("destroy")
                            .Description("Stops announcing when new users have joined this server.")
                            .Do(async e =>
                            {
                                _userJoinedSubs[e.Server.Id].IsEnabled = false;
                                await
                                    e.Channel.SendMessage(
                                        "Disabled user join messages. You can re-enable them at any time.");
                            });
                    });
                    // no join callback exists commands
                    joinGroup.CreateGroup("", doesntExistJoin =>
                    {
                        doesntExistJoin.AddCheck((cmd, usr, chnl) =>
                        {
                            if (!_userJoinedSubs.ContainsKey(chnl.Server.Id))
                                return true;

                            return !_userJoinedSubs[chnl.Server.Id].IsEnabled;
                        });

                        doesntExistJoin.CreateCommand("enable")
                            .Description("Enables announcing for when a new user joins this server.")
                            .Do(async e =>
                            {
                                if (_userJoinedSubs.ContainsKey(e.Server.Id))
                                    _userJoinedSubs[e.Server.Id].IsEnabled = true;
                                else
                                    _userJoinedSubs.TryAdd(e.Server.Id, new UserEventCallback(e.Channel, DefaultMessage));

                                await
                                    e.Channel.SendMessage(
                                        "Enabled user join messages.\r\nYou can now change the channel and the message by typing !help announce join.");
                            });
                    });
                });

                group.CreateGroup("leave", leaveGroup =>
                {
                    // joinGroup callback exists commands
                    leaveGroup.CreateGroup("", existsLeave =>
                    {
                        existsLeave.AddCheck((cmd, usr, chnl) =>
                        {
                            if (_userLeftSubs.ContainsKey(chnl.Server.Id))
                                return _userLeftSubs[chnl.Server.Id].IsEnabled;

                            return false;
                        });

                        existsLeave.CreateCommand("message")
                            .Description($"Sets the leave message for this current server.\r\n{SyntaxMessage}")
                            .Parameter("message", ParameterType.Unparsed)
                            .Do(async e =>
                            {
                                string msg = e.GetArg("message");
                                _userLeftSubs[e.Server.Id].Message = msg;
                                await e.Channel.SendMessage($"Set leave message to {msg}");
                            });
                        existsLeave.CreateCommand("channel")
                            .Description("Sets the callback channel for this servers leave announcements.")
                            .Parameter("channelName", ParameterType.Unparsed)
                            .Do(async e =>
                            {
                                string channelName = e.GetArg("channelName").ToLowerInvariant();
                                Channel channel =
                                    e.Server.TextChannels.FirstOrDefault(c => c.Name.ToLowerInvariant() == channelName);

                                if (channel == null)
                                {
                                    await e.Channel.SendMessage($"Channel with the name {channelName} was not found.");
                                    return;
                                }

                                _userLeftSubs[e.Server.Id].Channel = channel;
                                await e.Channel.SendMessage($"Set leave callback to channel {channel.Name}");
                            });
                        existsLeave.CreateCommand("destroy")
                            .Description("Stops announcing when users have left joined this server.")
                            .Do(async e =>
                            {
                                _userLeftSubs[e.Server.Id].IsEnabled = false;
                                await
                                    e.Channel.SendMessage(
                                        "Disabled user join messages. You can re-enable them at any time.");
                            });
                    });
                    // no leavea callback exists commands
                    leaveGroup.CreateGroup("", doesntExistLeave =>
                    {
                        doesntExistLeave.AddCheck((cmd, usr, chnl) =>
                        {
                            if (!_userLeftSubs.ContainsKey(chnl.Server.Id))
                                return true;

                            return !_userLeftSubs[chnl.Server.Id].IsEnabled;
                        });

                        doesntExistLeave.CreateCommand("enable")
                            .Description("Enables announcing for when a user leaves this server.")
                            .Do(async e =>
                            {
                                if (_userLeftSubs.ContainsKey(e.Server.Id))
                                    _userLeftSubs[e.Server.Id].IsEnabled = true;
                                else
                                    _userLeftSubs.TryAdd(e.Server.Id, new UserEventCallback(e.Channel, DefaultMessage));

                                await
                                    e.Channel.SendMessage(
                                        "Enabled user leave messages.\r\nYou can now change the channel and the message by typing !help announce leave.");
                            });
                    });
                });
            });
            manager.UserJoined += async (s, e) =>
            {
                if (!manager.EnabledServers.Contains(e.Server)) return;
                if (!_userJoinedSubs.ContainsKey(e.Server.Id)) return;

                UserEventCallback callback = _userJoinedSubs[e.Server.Id];
                if (callback.IsEnabled)
                    await callback.Channel.SendMessage(ParseString(callback.Message, e.User, e.Server));
            };

            manager.UserLeft += async (s, e) =>
            {
                if (!manager.EnabledServers.Contains(e.Server)) return;
                if (!_userLeftSubs.ContainsKey(e.Server.Id)) return;

                UserEventCallback callback = _userLeftSubs[e.Server.Id];
                if (callback.IsEnabled)
                    await callback.Channel.SendMessage(ParseString(callback.Message, e.User, e.Server));
            };
        }

        public void OnDataLoad()
        {
            if (_userJoinedSubs == null)
                _userJoinedSubs = new ConcurrentDictionary<ulong, UserEventCallback>();
            if (_userLeftSubs == null)
                _userLeftSubs = new ConcurrentDictionary<ulong, UserEventCallback>();

            LoadChannels(_userJoinedSubs);
            LoadChannels(_userLeftSubs);
        }

        private void LoadChannels(ConcurrentDictionary<ulong, UserEventCallback> dict)
        {
            foreach (var pair in dict)
                pair.Value.Channel = _client.GetServer(pair.Key).GetChannel(pair.Value.ChannelId);
        }

        private string ParseString(string input, User user, dynamic location)
            => input.Replace(UserNameKeyword, user.Name).Replace(LocationKeyword, location.Name);
    }
}
