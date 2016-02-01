using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using TerrariaBridge;
using TerrariaBridge.Client;

namespace Stormbot.Bot.Core.Modules
{
    public class TerrariaModule : IDataModule
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

            [JsonConstructor]
            private TerrChannelRelay(ulong channelId, string host, int port, string password)
            {
                ChannelId = channelId;
                Host = host;
                Password = password;
                Port = port;
            }

            public TerrChannelRelay(Channel channel, string host, int port, string password = null) : this(channel.Id, host, port, password)
            {
                Channel = channel;
            }

            public override bool Equals(object obj)
                => ChannelId == (obj as TerrChannelRelay)?.ChannelId;

            public override int GetHashCode()
            {
                unchecked
                {
                    return (int)ChannelId * 13;
                }
            }
        }

        [DataSave, DataLoad]
        private HashSet<TerrChannelRelay> _relays;

        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("terraria", mainGroup =>
            {
                // commands which can only be used when caller channel is connected to a terraria server.
                mainGroup.CreateGroup("", connectedGroup =>
                {
                    connectedGroup.AddCheck(
                        (cmd, usr, chnl) => GetRelay(chnl) != null);

                    /*
                      todo : add a command to check traveling merchant items
                      todo : command for getting player info
                      todo : command for getting world info
                    */
                    connectedGroup.CreateCommand("info")
                        .Description("Shows the info for the terraria server connected to this channel.")
                        .Do(async e =>
                        {
                            TerrChannelRelay relay = GetRelay(e.Channel);
                            await
                                e.Channel.SendMessage(
                                    $"This channel is connected to:\r\n```* Ip: {relay.Host}\r\n* Port:{relay.Port}\r\n* World: {relay.Client.World.WorldName}\r\n* Players: {relay.Client.Players.Count()}```");
                        });

                    connectedGroup.CreateCommand("disconnect")
                        .MinPermissions((int) PermissionLevel.ChannelModerator)
                        .Description("Disconnects from the terraria server connected to this channel.")
                        .Do(e =>
                        {
                            TerrChannelRelay relay = _relays.FirstOrDefault(r => r.ChannelId == e.Channel.Id);
                            CleanRelay(relay);
                        });

                });

                // commands which can only be used when caler channel is not connected to a terraria server.
                mainGroup.CreateGroup("", disconnectedGroup =>
                {
                    disconnectedGroup.AddCheck((cmd, usr, chnl) => GetRelay(chnl) == null);

                    disconnectedGroup.CreateCommand("connect")
                        .MinPermissions((int) PermissionLevel.ChannelModerator)
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

        private async Task StartClient(TerrChannelRelay relay)
        {
            try
            {
                if (relay.Channel == null)
                    relay.Channel = _client.GetChannel(relay.ChannelId);

                relay.Client = new TerrariaClient(cfg =>
                {
                    if (string.IsNullOrEmpty(relay.Password))
                        cfg.Password(relay.Password);

                    cfg.Player(player =>
                    {
                        player.Appearance(appear => appear.Name(relay.Channel.Name));
                        player.Buffs(buffs => buffs.Add(10)); // invis
                    });
                });

                // create event handlers
                relay.TerrariaMessageReceivedEvent = async (s, e) =>
                {
                    if (e.Message.Text.StartsWith(EscapePrefix)) return;

                    await relay.Channel.SendMessage($"**Terraria**: `<{e.Player.Appearance.Name}> {e.Message.Text}`");
                };
                relay.TerrariaDisconnectedEvent = async (s, e) =>
                {
                    await relay.Channel.SendMessage($"Disconnected from terraria server: `{e.Reason}`");
                    CleanRelay(relay);
                };
                relay.DiscordMessageReceivedEvent = (s, e) =>
                {
                    if (e.Channel.Id != relay.ChannelId) return;
                    if (e.Message.Text.StartsWith(EscapePrefix)) return;

                    char? commandChar = _client.Commands().Config.CommandChar;
                    if (commandChar != null)
                        if (e.Message.Text.StartsWith(commandChar.Value.ToString())) return;

                    if (e.Message.IsAuthor) return;

                    relay.Client.CurrentPlayer.SendMessage($"<{e.User.Name}> {e.Message.Text}");
                };

                //set handlers
                relay.Client.MessageReceived += relay.TerrariaMessageReceivedEvent;
                relay.Client.Disconnected += relay.TerrariaDisconnectedEvent;
                _client.MessageReceived += relay.DiscordMessageReceivedEvent;

                relay.Client.Log.MessageReceived +=
                    (s, e) => StrmyCore.Logger.FormattedWrite(e.Severity.ToString(), e.Message, ConsoleColor.White);

                relay.Client.ConnectAndLogin(relay.Host, relay.Port);

                await relay.Channel.SendMessage($"Connected to `{relay.Host}:{relay.Port}`");
            }
            catch (Exception ex)
            {
                await relay.Channel.SendMessage(
                    $"Couldn't relay terraria server at {relay.Host}:{relay.Port}. Exception:\r\n`{ex.Message}`");
                CleanRelay(relay);
            }
        }

        private void CleanRelay(TerrChannelRelay relay)
        {
            if (!_relays.Contains(relay))
            {
                Console.WriteLine($"CleanRelay treid to clean unregisted relay.");
                return;
            }

            relay.Client.MessageReceived -= relay.TerrariaMessageReceivedEvent;
            relay.Client.Disconnected -= relay.TerrariaDisconnectedEvent;
            _client.MessageReceived -= relay.DiscordMessageReceivedEvent;

            _relays.Remove(relay);
            relay.Client.SocketDispose();
        }

        public void OnDataLoad()
        {
            if(_relays == null)
                _relays = new HashSet<TerrChannelRelay>();

            Task.Run(async () =>
            {
                foreach (TerrChannelRelay relay in _relays)
                    await StartClient(relay);
            });
        }
    }
}
