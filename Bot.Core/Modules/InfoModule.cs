using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class InfoModule : IModule
    {
        public void Install(ModuleManager manager)
        {
            manager.CreateCommands("", group =>
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
                group.CreateCommand("info")
                    .Description("Displays information about the bot.")
                    .Do(async e =>
                    {
                        User owner = manager.Client.GetUser(Constants.UserOwner);
                        StringBuilder builder = new StringBuilder("**Bot info:\r\n**");
                        builder.AppendLine("- Owner: " + (owner == null ? "Not found." : $"{owner.Name} ({owner.Id})"));
                        builder.AppendLine($"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}");
                        builder.AppendLine("- GitHub: https://github.com/SSStormy/Stormbot");
                        builder.AppendLine($"- Memory Usage: {Math.Round(GC.GetTotalMemory(false)/(1024.0*1024.0), 2)} MB");

                        await e.Channel.SendMessage(builder.ToString());
                    });
            });
        }

        private async Task PrintUserInfo(User user, Channel textChannel)
        {
            if (user == null)
            {
                await textChannel.SendMessage("User not found.");
                return;
            }

            StringBuilder builder = new StringBuilder("**User info:\r\n**");
            builder.AppendLine($"- Name: {user.Name} ({user.Discriminator})");
            builder.AppendLine($"- Id: {user.Id}");
            builder.AppendLine($"- Avatar: {user.AvatarUrl}");
            await textChannel.SendMessage(builder.ToString());
        }
    }
}