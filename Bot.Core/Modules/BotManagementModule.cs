using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;

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
                    .Do(async e => await DiscordUtils.JoinInvite(e.GetArg("invite"), e.Channel));

                group.CreateCommand("leave")
                    .Description("Instructs the bot to leave this server.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(async e => await e.Server.Leave());

                group.CreateCommand("io save")
                    .Description("Saves data used by the bot.")
                    .Do(e => io.Save());

                group.CreateCommand("io load")
                    .Description("Loads data that the bot loads at runtime.")
                    .Do(e => io.Load());

                group.CreateCommand("set name")
                    .Description("Changes the name of the bot")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e => await _client.CurrentUser.Edit(Constants.Pass, e.GetArg("name")));

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
                    .Parameter("game", ParameterType.Unparsed)
                    .Do(e => _client.SetGame(e.GetArg("game")));

                group.CreateCommand("killbot")
                    .Description("Kills the bot.")
                    .Do(x =>
                    {
                        io.Save();
                        Environment.Exit(0);
                    });

                group.CreateCommand("cleanmsg")
                    .Description("Removes the last 100 messages sent by the bot in this channel.")
                    .MinPermissions((int)PermissionLevel.ChannelModerator)
                    .Do(async e =>
                    {
                        foreach(Message msg in (await e.Channel.DownloadMessages()).Where(m => m.IsAuthor))
                            await msg.Delete();
                    });

                group.CreateCommand("gc")
                   .Description("Lists used memory, then collects it.")
                   .Do(async e =>
                   {
                       await GetMemUsage(e);
                       await Collect(e);
                   });

                group.CreateCommand("gc collect")
                    .Description("Calls GC.Collect()")
                    .Do(async e => await Collect(e));

                group.CreateCommand("gc list")
                    .MinPermissions((int) PermissionLevel.User)
                    .Description("Calls GC.GetTotalMemory()")
                    .Do(async e => await GetMemUsage(e));
            });

            manager.MessageReceived += async (s, e) =>
            {
                if (!e.Channel.IsPrivate) return;

                if (e.Message.Text.StartsWith("https://discord.gg/"))
                {
                    string invite = String.Empty;
                    try
                    {
                        invite = e.Message.Text.Split(' ').FirstOrDefault();
                    }
                    catch { } // ignored

                    if (!string.IsNullOrEmpty(invite))
                        await DiscordUtils.JoinInvite(invite, e.Channel);
                }
            };
        }

        private async Task Collect(CommandEventArgs e)
        {
            double memoryBefore = GetMemoryUsage();
            GC.Collect();
            await
                e.Channel.SafeSendMessage(
                    $"Collected `{memoryBefore - GetMemoryUsage()} mb` of trash.");
        }

        private async Task GetMemUsage(CommandEventArgs e)
        {
            await e.Channel.SafeSendMessage($"Memory usage: `{GetMemoryUsage()} mb`");

        }

        private double GetMemoryUsage() => Math.Round(GC.GetTotalMemory(false) /(1024.0*1024.0), 2);
    }
}