using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Newtonsoft.Json;
using OpenTerrariaClient;
using OpenTerrariaClient.Client;
using OpenTerrariaClient.Model;
using OpenTerrariaClient.Model.ID;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Relay
{
    public class TerrariaRelayModule : IDataObject, IModule
    {
        private const string EscapePrefix = ".";

        [JsonObject(MemberSerialization.OptIn)]
        public class TerrChannelRelay
        {
            public TerrariaClient Client { get; set; }
            public Channel Channel { get; set; }

            [JsonProperty]
            public ulong ChannelId { get; }

            [JsonProperty]
            public string Host { get; }

            [JsonProperty]
            public string Password { get; }

            [JsonProperty]
            public int Port { get; }

            public EventHandler<MessageReceivedEventArgs> TerrariaMessageReceivedEvent;
            public EventHandler<DisconnectEventArgs> TerrariaDisconnectedEvent;
            public EventHandler<MessageEventArgs> DiscordMessageReceivedEvent;
            public EventHandler<LoggedInEventArgs> TerrariaOnLoginEvent;

            [JsonConstructor]
            private TerrChannelRelay(ulong channelId, string host, int port, string password)
            {
                ChannelId = channelId;
                Host = host;
                Password = password;
                Port = port;
            }

            public TerrChannelRelay(Channel channel, string host, int port, string password = null)
                : this(channel.Id, host, port, password)
            {
                Channel = channel;
            }

            public override bool Equals(object obj)
                => ChannelId == (obj as TerrChannelRelay)?.ChannelId;

            public override int GetHashCode()
            {
                unchecked
                {
                    return (int) ChannelId*13;
                }
            }
        }

        [DataSave, DataLoad] private HashSet<TerrChannelRelay> _relays;

        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateDynCommands("terraria", PermissionLevel.User, mainGroup =>
            {
                // commands which can only be used when caller channel is connected to a terraria server.
                mainGroup.CreateGroup("", connectedGroup =>
                {
                    connectedGroup.AddCheck(
                        (cmd, usr, chnl) => GetRelay(chnl) != null);

                    /*
                      todo : command for getting player info
                    */

                    connectedGroup.CreateCommand("info")
                        .Description("Shows the info for the terraria server connected to this channel.")
                        .Do(async e =>
                        {
                            TerrChannelRelay relay = GetRelay(e.Channel);
                            await
                                e.Channel.SafeSendMessage(
                                    $"This channel is connected to:\r\n```* Ip: {relay.Host}\r\n* Port:{relay.Port}\r\n* World: {relay.Client.World.WorldName}\r\n* Players: {relay.Client.Players.Count()}```");
                        });

                    connectedGroup.CreateCommand("disconnect")
                        .MinDynPermissions((int) PermissionLevel.ChannelModerator)
                        .Description("Disconnects from the terraria server connected to this channel.")
                        .Do(e =>
                        {
                            TerrChannelRelay relay = GetRelay(e.Channel);
                            CleanRelay(relay);
                        });

                    connectedGroup.CreateCommand("world")
                        .Description("Displays information about the servers world.")
                        .Do(async e =>
                        {
                            TerrChannelRelay relay = GetRelay(e.Channel);
                            StringBuilder builder = new StringBuilder($"**World data on {relay.Host}:{relay.Port}:**```");
                            builder.AppendLine($"- Name: {relay.Client.World.WorldName}");
                            builder.AppendLine($"- Time: {relay.Client.World.Time}");
                            builder.AppendLine($"- Is Raining: {relay.Client.World.Rain > 0}");
                            builder.AppendLine($"- Is Expert Mode: {relay.Client.World.IsExpertMode}");
                            builder.AppendLine($"- Is Hardmode: {relay.Client.World.IsHardmode}");
                            builder.AppendLine($"- Is Crimson: {relay.Client.World.IsCrimson}");
                            await e.Channel.SafeSendMessage($"{builder}```");
                        });

                    connectedGroup.CreateCommand("travmerch")
                        .Description(
                            "Displays what the travelling merchant has in stock if they are currently present in the world.")
                        .Do(async e =>
                        {
                            TerrChannelRelay relay = GetRelay(e.Channel);
                            Npc travelingMerchant =
                                relay.Client.Npcs.FirstOrDefault(n => n.NpcId == NpcId.TravellingMerchant);

                            if (travelingMerchant == null)
                            {
                                await
                                    e.Channel.SafeSendMessage(
                                        "The travelling merchant is not currently present in the world.");
                                return;
                            }

                            StringBuilder builder = new StringBuilder("**Travelling merchant stock:**```");

                            int index = 1;
                            foreach (GameItem item in travelingMerchant.Shop)
                            {
                                builder.AppendLine($"{index}: {item.Name()}");
                                index++;
                            }

                            await e.Channel.SafeSendMessage($"{builder}```");
                        });
                });

                // commands which can only be used when caller channel is not connected to a terraria server.
                mainGroup.CreateGroup("", disconnectedGroup =>
                {
                    disconnectedGroup.AddCheck((cmd, usr, chnl) => GetRelay(chnl) == null);

                    disconnectedGroup.CreateCommand("connect")
                        .MinDynPermissions((int) PermissionLevel.ChannelModerator)
                        .Description("Connects this channel to a terraria server.")
                        .Parameter("ip")
                        .Parameter("port")
                        .Parameter("password", ParameterType.Optional)
                        .Do(async e =>
                        {
                            TerrChannelRelay newRelay = new TerrChannelRelay(e.Channel, e.GetArg("ip"),
                                int.Parse(e.GetArg("port")), e.GetArg("password"));
                            _relays.Add(newRelay);
                            await StartClient(newRelay);
                        });
                });
            });
        }

        private TerrChannelRelay GetRelay(Channel channel)
            => _relays.FirstOrDefault(r => r.ChannelId == channel.Id);

        private async Task<bool> StartClient(TerrChannelRelay relay)
        {
            try
            {
                if (relay.Channel == null)
                    relay.Channel = _client.GetChannel(relay.ChannelId);

                if (relay.Channel == null)
                    return false;

                relay.Client = new TerrariaClient(cfg =>
                {
                    if (string.IsNullOrEmpty(relay.Password))
                        cfg.Password(relay.Password);

                    cfg.TrackItems(false);
                    cfg.TrackProjectiles(false);

                    cfg.Player(player =>
                    {
                        player.Appearance(appear => appear.Name(relay.Channel.Name));
                        player.Buffs(buffs => buffs.Add(BuffId.Invisibility));
                    });
                });

                // create event handlers
                relay.TerrariaMessageReceivedEvent = async (s, e) =>
                {
                    if (e.Message.Text.StartsWith(EscapePrefix)) return;

                    await
                        relay.Channel.SafeSendMessage($"**Terraria**: `<{e.Player.Appearance.Name}> {e.Message.Text}`");
                };
                relay.TerrariaDisconnectedEvent = async (s, e) =>
                {
                    await relay.Channel.SafeSendMessage($"Disconnected from terraria server: `{e.Reason}`");
                    CleanRelay(relay);
                };
                relay.DiscordMessageReceivedEvent = (s, e) =>
                {
                    if (e.Channel.Id != relay.ChannelId) return;
                    if (e.Message.Text.StartsWith(EscapePrefix)) return;

                    char? commandChar = _client.Commands().Config.PrefixChar;
                    if (commandChar != null)
                        if (e.Message.Text.StartsWith(commandChar.Value.ToString())) return;

                    if (e.Message.IsAuthor) return;

                    relay.Client.CurrentPlayer.SendMessage($"<{e.User.Name}> {e.Message.Text}");
                };

                relay.TerrariaOnLoginEvent = (s, e) => relay.Client.CurrentPlayer.Killme(" says hi.");

                //set handlers
                relay.Client.MessageReceived += relay.TerrariaMessageReceivedEvent;
                relay.Client.Disconnected += relay.TerrariaDisconnectedEvent;
                _client.MessageReceived += relay.DiscordMessageReceivedEvent;
                relay.Client.LoggedIn += relay.TerrariaOnLoginEvent;

                relay.Client.Log.MessageReceived +=
                    (s, e) => StrmyCore.Logger.FormattedWrite(e.Severity.ToString(), e.Message, ConsoleColor.White);

                relay.Client.ConnectAndLogin(relay.Host, relay.Port);

                await relay.Channel.SafeSendMessage($"Connected to `{relay.Host}:{relay.Port}`");

                return true;
            }
            catch (Exception ex)
            {
                await relay.Channel.SafeSendMessage(
                    $"Couldn't relay terraria server at {relay.Host}:{relay.Port}. Exception:\r\n`{ex.Message}`");
                CleanRelay(relay);
            }
            return false;
        }

        private void CleanRelay(TerrChannelRelay relay)
        {
            if (!_relays.Contains(relay))
            {
                Logger.FormattedWrite("TerrariaRelay", $"CleanRelay tried to clean unregisted relay.", ConsoleColor.Red);
                return;
            }

            relay.Client.MessageReceived -= relay.TerrariaMessageReceivedEvent;
            relay.Client.Disconnected -= relay.TerrariaDisconnectedEvent;
            _client.MessageReceived -= relay.DiscordMessageReceivedEvent;
            relay.Client.LoggedIn -= relay.TerrariaOnLoginEvent;

            _relays.Remove(relay);
            relay.Client.SocketDispose();
        }

        void IDataObject.OnDataLoad()
        {
            if (_relays == null)
                _relays = new HashSet<TerrChannelRelay>();

            Task.Run(async () =>
            {
                List<TerrChannelRelay> removeList = new List<TerrChannelRelay>();

                foreach (TerrChannelRelay relay in _relays)
                {
                    if (!await StartClient(relay))
                    {
                        Logger.FormattedWrite("TerrRelayLoad",
                            $"Tried to load TRelay for nonexistant channel id : {relay.ChannelId}. Removing",
                            ConsoleColor.Yellow);
                        removeList.Add(relay);
                    }
                }

                foreach (TerrChannelRelay relay in removeList)
                    _relays.Remove(relay);
            });
        }
    }
}

