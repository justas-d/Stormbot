// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Modules;
using Stormbot.Bot.Core.Modules.Audio;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core
{
    public class StormBot
    {
        private readonly string _email;
        private readonly string _password;

        private readonly DiscordClient _client = new DiscordClient(new DiscordConfig {LogLevel = LogSeverity.Debug});

        public StormBot(string email, string password)
        {
            _email = email;
            _password = password;
        }

        public void Start()
        {
            _client.Run(Init);
        }

        private async Task Init()
        {
            Logger.Writeline("Initializing Stormbot v2");
            Logger.Writeline("Installing services... ");
            _client.Services.Add(new HttpService());
            _client.Services.Add(new ModuleService());
            DataIoService io = _client.Services.Add(new DataIoService());
            _client.Services.Add(new CommandService(new CommandServiceConfig {HelpMode = HelpMode.Public})).CommandErrored +=
                delegate(object sender, CommandErrorEventArgs args)
                {
                    Logger.FormattedWrite("CommandService", $"CmdEx: {args.ErrorType} Ex: {args.Exception}", ConsoleColor.Red);
                };
            _client.Services.Add(new AudioService(new AudioServiceConfig {Channels = 2}));
            _client.Services.Add(new TwitchEmoteService());
            _client.Services.Add(new PermissionLevelService((u, c) =>
            {
                if (u.Id == Constants.UserOwner)
                    return (int) PermissionLevel.BotOwner;
                if (u.Server != null)
                {
                    if (Equals(u, c.Server.Owner))
                        return (int) PermissionLevel.ServerOwner;
                    ServerPermissions serverPerms = u.ServerPermissions;
                    if (serverPerms.ManageRoles)
                        return (int) PermissionLevel.ServerAdmin;
                    if (serverPerms.ManageMessages && serverPerms.KickMembers && serverPerms.BanMembers)
                        return (int) PermissionLevel.ServerModerator;
                    ChannelPermissions channelPerms = u.GetPermissions(c);
                    if (channelPerms.ManagePermissions)
                        return (int) PermissionLevel.ChannelAdmin;
                    if (channelPerms.ManageMessages)
                        return (int) PermissionLevel.ChannelModerator;
                    if (u.Roles.Any(r => r.Id == Constants.RoleTrusted))
                        return (int) PermissionLevel.Trusted;
                }
                return (int) PermissionLevel.User;
            }));

            Logger.Writeline("Connecting to Discord... ");
            await _client.Connect(_email, _password);

            Logger.Writeline("Installing modules... ");

            _client.AddModule<BotManagementModule>("Bot", ModuleFilter.ServerWhitelist | ModuleFilter.AlwaysAllowPrivate);
            _client.AddModule<ServerManagementModule>("Server Management", ModuleFilter.ServerWhitelist);
            _client.AddModule<AudioStreamModule>("Audio", ModuleFilter.ServerWhitelist);
            _client.AddModule<QualityOfLifeModule>("QoL", ModuleFilter.ServerWhitelist);
            _client.AddModule<TestModule>("Test", ModuleFilter.ServerWhitelist);
            _client.AddModule<TwitchModule>("Twitch", ModuleFilter.ServerWhitelist);
            _client.AddModule<InfoModule>("Information", ModuleFilter.ServerWhitelist | ModuleFilter.AlwaysAllowPrivate);
            _client.AddModule<ModulesModule>("Modules");

            _client.Log.Message += (sender, args) => Logger.DiscordLog(args);

            Logger.Writeline("Loading data... ");
            io.Load();


            Logger.Writeline($" -WE ARE LIVE-{Environment.NewLine}");
        }
    }
}
