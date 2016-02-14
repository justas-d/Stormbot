using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Modules;
using Stormbot.Bot.Core.Modules.Audio;
using Stormbot.Bot.Core.Modules.Relay;
using Stormbot.Bot.Core.Modules.Twitch;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core
{
    public class StormBot
    {
        private readonly string _email;
        private readonly string _password;

        private DiscordClient Client { get; }

        private readonly Dictionary<LogSeverity, ConsoleColor> _colorMap = new Dictionary<LogSeverity, ConsoleColor>()
        {
            {LogSeverity.Debug, ConsoleColor.DarkYellow},
            {LogSeverity.Error, ConsoleColor.Red },
            {LogSeverity.Info, ConsoleColor.Blue },
            {LogSeverity.Verbose, ConsoleColor.Gray },
            {LogSeverity.Warning, ConsoleColor.Yellow}
        };

        private readonly HashSet<LogSeverity> _ignoredLogs = new HashSet<LogSeverity>()
        {
            LogSeverity.Debug,
            LogSeverity.Verbose
        };

        public StormBot(string email, string password)
        {
            _email = email;
            _password = password;

            Client = new DiscordClient(config =>
            {
                config.LogLevel = LogSeverity.Debug;
            });
        }

        public void Start() => Client.ExecuteAndWait(Init);

        private async Task Init()
        {
            Logger.Writeline("Initializing Stormbot v2");
            Logger.Writeline("Installing services... ");

            Client.Services.Add(new HttpService());
            Client.Services.Add(new ModuleService());

            DataIoService io = Client.Services.Add(new DataIoService());

            Client.UsingCommands(cmd =>
            {
                cmd.AllowMentionPrefix = true;
                cmd.ErrorHandler += async (s, e) =>
                {
                    switch (e.ErrorType)
                    {
                        case CommandErrorType.Exception:
                            await
                                e.Channel.SendMessage(
                                    $"{e.User.Mention} Something went wrong while processing your command! Make sure your input is in the valid format.");
                            break;

                        case CommandErrorType.UnknownCommand:
                            await e.Channel.SendMessage($"{e.User.Mention} that command does not exist.");
                            break;

                        case CommandErrorType.BadPermissions:
                            StringBuilder builder = new StringBuilder($"{e.User.Mention} you do not have sufficient permissions for that command. ");
                            if (e.Exception != null && !string.IsNullOrEmpty(e.Exception.Message))
                                builder.AppendLine($"Error message: ```{e.Exception.Message}```");
                            await e.Channel.SendMessage(builder.ToString());
                            break;

                        case CommandErrorType.BadArgCount:
                            await
                                e.Channel.SendMessage(
                                    $"{e.User.Mention} bad argument count.");
                            break;

                        case CommandErrorType.InvalidInput:
                            await e.Channel.SendMessage($"{e.User.Mention} invalid command input.");
                            break;

                        default:
                            Logger.FormattedWrite("CommandService", $"e.ErrorType ({e.ErrorType}) is not handled.", ConsoleColor.Yellow);
                            break;
                    }
                };
                cmd.PrefixChar = '}';
                cmd.HelpMode = HelpMode.Public;
            });

            Client.UsingAudio(audio =>
            {
                audio.EnableMultiserver = true;
                audio.Mode = AudioMode.Outgoing;;
                audio.Channels = 2;
                audio.EnableEncryption = true;
            });

            Client.UsingPermissionLevels((u, c) =>
            {
                if (u.Id == Constants.UserOwner)
                    return (int)PermissionLevel.BotOwner;

                if (u.Server != null)
                {
                    if (Equals(u, c.Server.Owner))
                        return (int)PermissionLevel.ServerOwner;

                    ServerPermissions serverPerms = u.ServerPermissions;
                    if (serverPerms.ManageRoles)
                        return (int)PermissionLevel.ServerAdmin;
                    if (serverPerms.ManageMessages && serverPerms.KickMembers && serverPerms.BanMembers)
                        return (int)PermissionLevel.ServerModerator;

                    ChannelPermissions channelPerms = u.GetPermissions(c);
                    if (channelPerms.ManagePermissions)
                        return (int)PermissionLevel.ChannelAdmin;
                    if (channelPerms.ManageMessages)
                        return (int)PermissionLevel.ChannelModerator;
                }

                return (int)PermissionLevel.User;
            });

            Client.UsingDynamicPerms();

            Logger.Writeline("Connecting to Discord... ");
            await Client.Connect(_email, _password);

            Logger.Writeline("Installing modules... ");

            Client.AddModule<BotManagementModule>("Bot", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist | ModuleFilter.AlwaysAllowPrivate);
            Client.AddModule<ServerManagementModule>("Server Management", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist);
            Client.AddModule<AudioStreamModule>("Audio", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist);
            Client.AddModule<QualityOfLifeModule>("QoL", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist);
            Client.AddModule<TestModule>("Test", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist);
            Client.AddModule<InfoModule>("Information", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist | ModuleFilter.AlwaysAllowPrivate);
            Client.AddModule<ModulesModule>("Modules");
            Client.AddModule<ExecuteModule>("Execute", ModuleFilter.AlwaysAllowPrivate);
            Client.AddModule<TerrariaRelayModule>("Terraria Relay", ModuleFilter.ChannelWhitelist | ModuleFilter.ServerWhitelist);
            Client.AddModule<TwitchRelayModule>("Twitch Relay", ModuleFilter.ChannelWhitelist | ModuleFilter.ServerWhitelist);
            Client.AddModule<TwitchEmoteModule>("Twitch Emotes", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist);
            Client.AddModule<AnnouncementModule>("Announcements", ModuleFilter.ServerWhitelist);
            Client.AddModule<VermintideModule>("Vermintide", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist | ModuleFilter.AlwaysAllowPrivate);
            Client.AddModule<PersonalModule>("Personal", ModuleFilter.ServerWhitelist);
            //Client.AddModule<GameModule>("Game", ModuleFilter.ServerWhitelist | ModuleFilter.ChannelWhitelist | ModuleFilter.AlwaysAllowPrivate);
            Client.Log.Message += (sender, args) =>
            {
                if (_ignoredLogs.Contains(args.Severity)) return;

                Logger.FormattedWrite($"{args.Severity} {args.Source}", $"{args.Message}", _colorMap[args.Severity]);

                if(args.Exception != null)
                    Logger.Write($"Exception: {args.Exception}");
            };

            Logger.Writeline("Loading data... ");
            io.Load();

            Client.SetGame("}help for commands");
            Constants.Owner = Client.GetUser(Constants.UserOwner);

            Logger.Writeline($" -WE ARE LIVE-{Environment.NewLine}");
        }
    }
}
