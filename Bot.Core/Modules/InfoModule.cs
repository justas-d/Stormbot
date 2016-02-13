using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class InfoModule : IModule
    {
        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateDynCommands("", PermissionLevel.User, group =>
            {
                group.CreateCommand("whois")
                    .Description("Displays information about the given user.")
                    .Parameter("username")
                    .Do(async e =>
                    {
                        await PrintUserInfo(e.Server.FindUsers(e.GetArg("username")).FirstOrDefault(), e.Channel);
                    });

                group.CreateCommand("whoami")
                    .Description("Displays information about the callers user.")
                    .Do(async e =>
                    {
                        await PrintUserInfo(e.User, e.Channel);
                    });
                group.CreateCommand("chatinfo")
                    .Description("Displays information about the current chat channel.")
                    .Do(async e =>
                    {
                        Channel channel = e.Channel;
                        StringBuilder builder = new StringBuilder($"**Info for {channel.Name}:\r\n**```");
                        builder.AppendLine($"- Id: {channel.Id}");
                        builder.AppendLine($"- Position: {channel.Position}");
                        builder.AppendLine($"- Parent server id: {channel.Server.Id}");

                        await e.Channel.SafeSendMessage($"{builder}```");
                    });
                group.CreateCommand("info")
                    .Description("Displays information about the bot.")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder("**Bot info:\r\n**```");
                        Tuple<int, int> serverData = GetServerData();

                        builder.AppendLine("- Owner: " + (Constants.Owner == null ? "Not found." : $"{Constants.Owner.Name} ({Constants.Owner.Id})"));
                        builder.AppendLine($"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}");
                        builder.AppendLine("- GitHub: https://github.com/SSStormy/Stormbot");
                        builder.AppendLine($"- Memory Usage: {Math.Round(GC.GetTotalMemory(false)/(1024.0*1024.0), 2)} MB");
                        builder.AppendLine($"- Ffmpeg Process count: {Constants.FfmpegProcessCount}");
                        builder.AppendLine($"- Servers: {_client.Servers.Count()}");
                        builder.AppendLine($"- Channels: {serverData.Item1}");
                        builder.AppendLine($"- Users: {serverData.Item2}");

                        await e.Channel.SafeSendMessage($"{builder}```");
                    });

                group.CreateCommand("contact")
                    .Description("Contact @SSStormy")
                    .Do(async e =>
                    {
                        await
                            e.Channel.SafeSendMessage(
                                $"**Reach my developer at**:\r\n" +
                                $"- http://steamcommunity.com/profiles/76561198035041409/");
                    });
            });
        }

        // int1: channel count: int2: user count.
        private Tuple<int, int> GetServerData()
        {
            int channels = 0;
            int users = 0;

            foreach (Server server in _client.Servers)
            {
                channels += server.TextChannels.Count();
                users += server.Users.Count();
            }

            return Tuple.Create(channels, users);
        }

        private async Task PrintUserInfo(User user, Channel textChannel)
        {
            if (user == null)
            {
                await textChannel.SafeSendMessage("User not found.");
                return;
            }

            StringBuilder builder = new StringBuilder("**User info:\r\n**```");
            builder.AppendLine($"- Name: {user.Name} ({user.Discriminator})");
            builder.AppendLine($"- Id: {user.Id}");
            builder.AppendLine($"- Avatar: {user.AvatarUrl}");
            builder.AppendLine($"- Joined: {user.JoinedAt} ");
            await textChannel.SafeSendMessage($"{builder}```");
        }
    }
}