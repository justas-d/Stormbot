using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    public class BotManagementModule : IModule
    {
        private DiscordClient _client;
        private DataIoService _io;

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;
            _client.MessageReceived +=
                (s, e) =>
                {
                    StringBuilder builder = new StringBuilder($"Msg: ");

                    if (e.Server != null)
                        builder.Append($"{e.Server.Name} ");

                    builder.Append(e.Channel.Name);

                    Logger.FormattedWrite(builder.ToString(), e.Message.ToString(),
                        ConsoleColor.White);
                };

            _io = _client.GetService<DataIoService>();

            manager.CreateCommands("", group =>
            {
                group.MinPermissions((int) PermissionLevel.BotOwner);

                group.CreateCommand("io save")
                    .Description("Saves data used by the bot.")
                    .Do(e => _io.Save());

                group.CreateCommand("io load")
                    .Description("Loads data that the bot loads at runtime.")
                    .Do(e => _io.Load());

                group.CreateCommand("set name")
                    .Description("Changes the name of the bot")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e => await _client.CurrentUser.Edit(Config.Pass, e.GetArg("name")));

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

                            await _client.CurrentUser.Edit(Config.Pass, avatar: stream, avatarType: type);
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
                        _io.Save();
                        Environment.Exit(0);
                    });

                group.CreateCommand("gc")
                    .Description("Lists used memory, then collects it.")
                    .Do(async e =>
                    {
                        await PrintMemUsage(e);
                        await Collect(e);
                    });

                group.CreateCommand("gc collect")
                    .Description("Calls GC.Collect()")
                    .Do(async e => await Collect(e));

                group.CreateCommand("gencmdmd")
                    .AddCheck((cmd, usr, chnl) => !chnl.IsPrivate)
                    .Do(async e => await DiscordUtils.GenerateCommandMarkdown(_client));
            });

            manager.CreateDynCommands("", PermissionLevel.User, group =>
            {
                group.CreateCommand("join")
                    .Description("Joins a server by invite.")
                    .Parameter("invite")
                    .Do(async e => await DiscordUtils.JoinInvite(e.GetArg("invite"), e.Channel));

                group.CreateCommand("leave")
                    .Description("Instructs the bot to leave this server.")
                    .MinDynPermissions((int) PermissionLevel.ServerModerator)
                    .Do(async e => await e.Server.Leave());

                group.CreateCommand("cleanmsg")
                    .Description("Removes the last 100 messages sent by the bot in this channel.")
                    .MinPermissions((int) PermissionLevel.ChannelModerator)
                    .Do(async e =>
                    {
                        foreach (Message msg in (await e.Channel.DownloadMessages()).Where(m => m.IsAuthor))
                            await msg.Delete();
                    });

                group.CreateCommand("gc list")
                    .Description("Calls GC.GetTotalMemory()")
                    .Do(async e => await PrintMemUsage(e));
            });

            manager.MessageReceived += async (s, e) =>
            {
                if (!e.Channel.IsPrivate) return;

                if (e.Message.Text.StartsWith("https://discord.gg/"))
                {
                    string invite = string.Empty;
                    try
                    {
                        invite = e.Message.Text.Split(' ').FirstOrDefault();
                    }
                    catch
                    {
                    } // ignored

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

        private async Task PrintMemUsage(CommandEventArgs e)
            => await e.Channel.SafeSendMessage($"Memory usage: `{GetMemoryUsage()} mb`");

        private double GetMemoryUsage()
            => Math.Round(GC.GetTotalMemory(false)/(1024.0*1024.0), 2);
    }
}