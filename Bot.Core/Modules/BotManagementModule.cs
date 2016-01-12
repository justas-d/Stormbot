// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class BotManagementModule : IModule
    {
        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _client.MessageReceived +=
                (sender, e) => Logger.FormattedWrite("Msg", e.Message.ToString(), ConsoleColor.White);

            DataIoService io = _client.Services.Get<DataIoService>();

            manager.CreateCommands("", group =>
            {
                group.MinPermissions((int) PermissionLevel.BotOwner);

                group.CreateCommand("join")
                    .MinPermissions((int) PermissionLevel.User)
                    .Description("Joins a server by invite.")
                    .Parameter("invite")
                    .Do(async e =>
                    {
                        Invite invite = await _client.GetInvite(e.GetArg("invite"));
                        if (invite == null)
                        {
                            await e.Channel.SendMessage("Invite not found.");
                            return;
                        }
                        else if (invite.IsRevoked)
                        {
                            await
                                e.Channel.SendMessage("This invite has expired or the bot is banned from that server.");
                            return;
                        }

                        await invite.Accept();
                        await e.Channel.SendMessage("Joined server.");
                    });

                group.CreateCommand("leave")
                    .Description("Instructs the bot to leave this server.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .MinPermissions((int) PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("Leaving~");
                        await e.Server.Leave();
                    });

                group.CreateCommand("io save")
                    .Description("Saves data used by the bot.")
                    .Do(e =>
                    {
                        io.Save();
                    });

                group.CreateCommand("io load")
                    .Description("Loads data that the bot loads at runtime.")
                    .Do(e =>
                    {
                        io.Load();
                    });

                group.CreateCommand("set name")
                    .Description("Changes the name of the bot")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await _client.CurrentUser.Edit(Constants.Pass, e.GetArg("name"));
                    });

                group.CreateCommand("set avatar")
                    .Description("Changes the avatar of the bot")
                    .Parameter("name")
                    .Do(async e =>
                    {
                        string avDir = Constants.DataFolderDir + e.GetArg("name");

                        using (FileStream stream = new FileStream(avDir, FileMode.Open))
                        {
                            ImageType type;
                            switch (Path.GetExtension(avDir))
                            {
                                case ".jpg":
                                case ".jpeg":
                                    type = ImageType.Jpeg;
                                    break;
                                case ".png":
                                    type = ImageType.Png;
                                    break;
                                default:
                                    return;
                            }

                            await _client.CurrentUser.Edit(Constants.Pass, avatar: stream, avatarType: type);
                            stream.Close();
                            stream.Dispose();
                        }
                    });

                group.CreateCommand("set game")
                    .Description("Sets the current played game for the bot.")
                    .Parameter("game")
                    .Do(async e =>
                    {
                        string game = e.GetArg("game");
                        _client.SetGame(game);
                        await e.User.SendMessage($"Set game to `{game}`");
                    });
                group.CreateCommand("killbot")
                    .Description("Kills the bot.")
                    .Do(x =>
                    {
                        io.Save();
                        Environment.Exit(0);
                    });
            });

            manager.CreateCommands("gc", group =>
            {
                group.MinPermissions((int) PermissionLevel.BotOwner);

                group.CreateCommand("")
                    .Description("Lists used memory, then collects it.")
                    .Do(async e =>
                    {
                        await GetMemUsage(e);
                        await Collect(e);
                    });

                group.CreateCommand("collect")
                    .Description("Calls GC.Collect()")
                    .Do(async e =>
                    {
                        await Collect(e);
                    });
                group.CreateCommand("list")
                    .MinPermissions((int) PermissionLevel.User)
                    .Description("Calls GC.GetTotalMemory()")
                    .Do(async e =>
                    {
                        await GetMemUsage(e);
                    });
            });
        }

        private async Task Collect(CommandEventArgs e)
        {
            long bytesBefore = GC.GetTotalMemory(false);
            GC.Collect();
            await
                e.Channel.SendMessage(
                    $"Collected `{((bytesBefore - GC.GetTotalMemory(false))/1024f)/1024f} mb` of trash.");
        }

        private async Task GetMemUsage(CommandEventArgs e)
        {
            await e.Channel.SendMessage($"Memory usage: `{(GC.GetTotalMemory(false)/1024f)/1024f} mb`");

        }
    }
}