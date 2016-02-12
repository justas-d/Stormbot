using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using StrmyCore;

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

        [JsonObject(MemberSerialization.OptIn)]
        private class AnnounceChannelData
        {
            [JsonProperty]
            public JsonChannel Channel { get; private set; }

            [JsonProperty]
            public bool IsEnabled { get; set; }

            [JsonConstructor]
            private AnnounceChannelData(JsonChannel channel, bool isEnabled)
            {
                Channel = channel;
                IsEnabled = isEnabled;
            }

            public AnnounceChannelData(Channel channel) : this(channel, true)
            {

            }
        }

        [DataSave, DataLoad] private ConcurrentDictionary<ulong, UserEventCallback> _userJoinedSubs;
        [DataSave, DataLoad] private ConcurrentDictionary<ulong, UserEventCallback> _userLeftSubs;
        [DataSave, DataLoad] private ConcurrentDictionary<ulong, ulong> _joinedRoleSubs;
        [DataSave, DataLoad] private ConcurrentDictionary<ulong, AnnounceChannelData> _defaultAnnounceChannels;

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
                group.CreateCommand("disable")
                    .AddCheck((cmd, usr, chnl) =>
                    {
                        if (_defaultAnnounceChannels.ContainsKey(chnl.Server.Id))
                            return _defaultAnnounceChannels[chnl.Server.Id].IsEnabled;

                        return true;
                    })
                    .Description("Disables but owner announcements on this server.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(async e =>
                    {
                        if (_defaultAnnounceChannels.ContainsKey(e.Server.Id))
                            _defaultAnnounceChannels[e.Server.Id].IsEnabled = false;
                        else
                            _defaultAnnounceChannels.TryAdd(e.Server.Id,
                                new AnnounceChannelData(e.Server.DefaultChannel) {IsEnabled = false});

                        await e.Channel.SendMessage("Disabled owner announcements for this server.");
                    });

                group.CreateCommand("enable")
                    .AddCheck((cmd, usr, chnl) =>
                    {
                        if (_defaultAnnounceChannels.ContainsKey(chnl.Server.Id))
                            return !_defaultAnnounceChannels[chnl.Server.Id].IsEnabled;

                        return false;
                    })
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Description("Enabled but owner announcements on this server.")
                    .Do(async e =>
                    {
                        _defaultAnnounceChannels[e.Server.Id].IsEnabled = true;
                        await e.Channel.SendMessage("Enabled owner announcements for this server.");
                    });

                group.CreateCommand("channel")
                    .Description("Sets the default channel of any announcements from the bot's owner.")
                    .Parameter("channelname", ParameterType.Unparsed)
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(async e =>
                    {
                        string channelQuery = e.GetArg("channelname").ToLowerInvariant();
                        Channel channel =
                            e.Server.TextChannels.FirstOrDefault(c => c.Name.ToLowerInvariant() == channelQuery);

                        if (channel == null)
                        {
                            await e.Channel.SafeSendMessage($"Channel with the name of `{channelQuery}` wasn't found.");
                            return;
                        }

                        AnnounceChannelData newVal = new AnnounceChannelData(channel);
                        _defaultAnnounceChannels.AddOrUpdate(e.Server.Id, newVal, (k, v) => newVal);

                        await e.Channel.SendMessage($"Set annoucement channel to `{channel.Name}`");
                    });

                group.CreateCommand("current")
                    .Description("Returns the current announcement channel.")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder("**Announcement channel**: ");

                        if (!_defaultAnnounceChannels.ContainsKey(e.Server.Id))
                            builder.Append(e.Server.DefaultChannel);
                        else
                            builder.Append(_defaultAnnounceChannels[e.Server.Id].Channel.Channel.Name);

                        await e.Channel.SafeSendMessage(builder.ToString());
                    });

                group.CreateCommand("message")
                    .MinPermissions((int) PermissionLevel.BotOwner)
                    .Parameter("msg", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string message = e.GetArg("msg");

                        foreach (Server server in _client.Servers)
                        {
                            AnnounceChannelData announceData;
                            _defaultAnnounceChannels.TryGetValue(server.Id, out announceData);

                            if (announceData == null)
                                await server.DefaultChannel.SafeSendMessage(message);
                            else
                            {
                                if (announceData.IsEnabled)
                                    await _defaultAnnounceChannels[server.Id].Channel.Channel.SafeSendMessage(message);
                            }
                        }
                    });
            });

            manager.CreateCommands("autorole", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerAdmin);

                // commands that are available when the server doesnt have an auto role set on join.
                group.CreateGroup("", noSubGroup =>
                {
                    noSubGroup.AddCheck((cmd, usr, chnl) => !_joinedRoleSubs.ContainsKey(chnl.Server.Id));

                    noSubGroup.CreateCommand("create")
                        .Description("Enables the bot to add a given role to newly joined users.")
                        .Parameter("rolename", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            string roleQuery = e.GetArg("rolename");
                            Role role = e.Server.FindRoles(roleQuery).FirstOrDefault();

                            if (role == null)
                            {
                                await e.Channel.SafeSendMessage($"A role with the name of `{roleQuery}` was not found.");
                                return;
                            }

                            _joinedRoleSubs.TryAdd(e.Server.Id, role.Id);

                            await
                                e.Channel.SafeSendMessage(
                                    $"Created an auto role asigned for new users. Role: {role.Name}");
                        });
                });

                // commands that are available when the server does have an auto role set on join.
                group.CreateGroup("", subGroup =>
                {
                    subGroup.AddCheck((cmd, usr, chnl) => (_joinedRoleSubs.ContainsKey(chnl.Server.Id)));

                    subGroup.CreateCommand("destroy")
                        .Description("Destoys the auto role assigner for this server.")
                        .Do(e => RemoveAutoRoleAssigner(e.Server.Id, e.Channel));

                    subGroup.CreateCommand("role")
                        .Parameter("rolename", ParameterType.Unparsed)
                        .Description("Changes the role of the auto role assigner for this server.")
                        .Do(async e =>
                        {
                            string roleQuery = e.GetArg("rolename");
                            Role role = e.Server.FindRoles(roleQuery, false).FirstOrDefault();

                            if (role == null)
                            {
                                await e.Channel.SafeSendMessage($"A role with the name of `{roleQuery}` was not found.");
                                return;
                            }
                            _joinedRoleSubs[e.Server.Id] = role.Id;

                            await e.Channel.SafeSendMessage($"Set the auto role assigner role to `{role.Name}`.");
                        });
                });
            });

            manager.CreateCommands("newuser", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerModerator);

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
                                await e.Channel.SafeSendMessage($"Set join message to {msg}");
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
                                    await
                                        e.Channel.SafeSendMessage($"Channel with the name {channelName} was not found.");
                                    return;
                                }

                                _userJoinedSubs[e.Server.Id].Channel = channel;
                                await e.Channel.SafeSendMessage($"Set join callback to channel {channel.Name}");
                            });
                        existsJoin.CreateCommand("destroy")
                            .Description("Stops announcing when new users have joined this server.")
                            .Do(async e =>
                            {
                                _userJoinedSubs[e.Server.Id].IsEnabled = false;
                                await
                                    e.Channel.SafeSendMessage(
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
                                    e.Channel.SafeSendMessage(
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
                                await e.Channel.SafeSendMessage($"Set leave message to {msg}");
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
                                    await
                                        e.Channel.SafeSendMessage($"Channel with the name {channelName} was not found.");
                                    return;
                                }

                                _userLeftSubs[e.Server.Id].Channel = channel;
                                await e.Channel.SafeSendMessage($"Set leave callback to channel {channel.Name}");
                            });
                        existsLeave.CreateCommand("destroy")
                            .Description("Stops announcing when users have left joined this server.")
                            .Do(async e =>
                            {
                                _userLeftSubs[e.Server.Id].IsEnabled = false;
                                await
                                    e.Channel.SafeSendMessage(
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
                                    e.Channel.SafeSendMessage(
                                        "Enabled user leave messages.\r\nYou can now change the channel and the message by typing !help announce leave.");
                            });
                    });
                });
            });
            manager.UserJoined += async (s, e) =>
            {
                if (!manager.EnabledServers.Contains(e.Server)) return;
                if (_userJoinedSubs.ContainsKey(e.Server.Id))
                {
                    UserEventCallback callback = _userJoinedSubs[e.Server.Id];
                    if (callback.IsEnabled)
                        await callback.Channel.SafeSendMessage(ParseString(callback.Message, e.User, e.Server));
                }

                if (_joinedRoleSubs.ContainsKey(e.Server.Id))
                {
                    // verify that the role still exists.
                    Role role = e.Server.GetRole(_joinedRoleSubs[e.Server.Id]);

                    if (role == null)
                    {
                        await RemoveAutoRoleAssigner(e.Server.Id, null, false);

                        Channel callback = e.Server.TextChannels.FirstOrDefault();
                        if (callback != null)
                            await
                                callback.SafeSendMessage("Auto role assigner was given a non existant role. Removing.");

                        return;
                    }

                    await e.User.SafeAddRoles(e.Server.CurrentUser, role);
                }
            };

            manager.UserLeft += async (s, e) =>
            {
                if (!manager.EnabledServers.Contains(e.Server)) return;
                if (!_userLeftSubs.ContainsKey(e.Server.Id)) return;

                UserEventCallback callback = _userLeftSubs[e.Server.Id];
                if (callback.IsEnabled)
                    await callback.Channel.SafeSendMessage(ParseString(callback.Message, e.User, e.Server));
            };
        }

        private async Task RemoveAutoRoleAssigner(ulong serverId, Channel callback, bool shouldCallback = true)
        {
            ulong ignored;
            _joinedRoleSubs.TryRemove(serverId, out ignored);

            if (shouldCallback)
                await callback.SafeSendMessage("Removed auto role assigner for this server.");
        }

        public void OnDataLoad()
        {
            if (_userJoinedSubs == null)
                _userJoinedSubs = new ConcurrentDictionary<ulong, UserEventCallback>();
            if (_userLeftSubs == null)
                _userLeftSubs = new ConcurrentDictionary<ulong, UserEventCallback>();
            if (_joinedRoleSubs == null)
                _joinedRoleSubs = new ConcurrentDictionary<ulong, ulong>();
            if (_defaultAnnounceChannels == null)
                _defaultAnnounceChannels = new ConcurrentDictionary<ulong, AnnounceChannelData>();

            LoadChannels(_userJoinedSubs);
            LoadChannels(_userLeftSubs);

            foreach (var pair in _defaultAnnounceChannels)
            {
                if (!pair.Value.Channel.FinishLoading(_client))
                {
                    Logger.FormattedWrite("AnnounceLoad",
                        $"Failed loading JsonChannel id {pair.Value.Channel.ChannelId}. Removing", ConsoleColor.Yellow);
                    _defaultAnnounceChannels.Remove(pair.Key);
                }
            }
        }

        private void LoadChannels(ConcurrentDictionary<ulong, UserEventCallback> dict)
        {
            foreach (var pair in dict)
            {
                try
                {
                    pair.Value.Channel = _client.GetServer(pair.Key).GetChannel(pair.Value.ChannelId);
                }
                catch (NullReferenceException)
                {
                    Logger.FormattedWrite("AnnounceLoad",
                        $"Failed loading channel {pair.Value.ChannelId} for server {pair.Key}. Removing",
                        ConsoleColor.Yellow);
                    dict.Remove(pair.Key);
                }
            }
        }

        private string ParseString(string input, User user, dynamic location)
            => input.Replace(UserNameKeyword, user.Name).Replace(LocationKeyword, location.Name);
    }
}
