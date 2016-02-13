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
                        User owner = manager.Client.GetUser(Constants.UserOwner);
                        StringBuilder builder = new StringBuilder("**Bot info:\r\n**```");
                        builder.AppendLine("- Owner: " + (owner == null ? "Not found." : $"{owner.Name} ({owner.Id})"));
                        builder.AppendLine($"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}");
                        builder.AppendLine("- GitHub: https://github.com/SSStormy/Stormbot");
                        builder.AppendLine($"- Memory Usage: {Math.Round(GC.GetTotalMemory(false)/(1024.0*1024.0), 2)} MB");
                        builder.AppendLine($"- Ffmpeg Process count: {Constants.FfmpegProcessCount}");

                        await e.Channel.SafeSendMessage($"{builder}```");
                    });

                group.CreateCommand("contact")
                    .Description("Contact @SSStormy")
                    .Do(async e =>
                    {
                        await
                            e.Channel.SafeSendMessage(
                                $"**Reach my developer at**:\r\n" +
                                $"- <@{Constants.UserOwner}> <- click for PM Channel!");
                    });
            });
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