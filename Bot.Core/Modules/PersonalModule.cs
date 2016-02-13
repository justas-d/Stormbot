using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    public class PersonalModule : IModule
    {
        private const ulong TwinkChannelId = 131490784544292865;
        private const ulong RebbitId = 131468210531860480;
        private const ulong CrixiusId = 131465844176715776;

        public void Install(ModuleManager manager)
        {
            manager.CreateCommands("", group =>
            {
                group.CreateCommand("qupte")
                    .Description("Ruby for fucks sake...")
                    .MinPermissions((int) PermissionLevel.User)
                    .Do(async e =>
                    {
                        const string quptePoolDir = Constants.DataFolderDir + @"12\";
                        if (!Directory.Exists(quptePoolDir)) return;

                        await e.Channel.SendFile(Directory.GetFiles(quptePoolDir).PickRandom());
                    });

                group.CreateCommand("twink")
                    .Description("Moves Rebbit and Crixius to the Portuguese Twink Containment Zone TM (R) (c)")
                    .Do(async e =>
                    {
                        Channel channel = e.Server.GetChannel(TwinkChannelId);
                        await MoveToVoice(channel,
                            e.Server.GetUser(CrixiusId),
                            e.Server.GetUser(RebbitId));
                    });
            });
        }

        private async Task MoveToVoice(Channel voiceChannel, params User[] users)
        {
            foreach (User user in users.Where(user => user.Status == UserStatus.Online ||
                                                      user.Status == UserStatus.Idle))
            {
                await user.Edit(voiceChannel: voiceChannel);
            }
        }
    }
}
