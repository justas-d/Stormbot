using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using JetBrains.Annotations;
using Newtonsoft.Json;
using RestSharp.Extensions.MonoHttp;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    public class QualityOfLifeModule : IDataModule
    {
        [Serializable]
        private class ReminderData
        {
            public ulong User { get; }
            public DateTime EndTime { get; }
            public DateTime CreateTime { get; }
            public string Reason { get; }

            public ReminderData(ulong user, TimeSpan span, string reason)
            {
                User = user;
                EndTime = DateTime.Now + span;
                CreateTime = DateTime.Now;
                Reason = reason;
            }

            [JsonConstructor, UsedImplicitly]
            private ReminderData(ulong user, DateTime endTime, DateTime createTime, string reason)
            {
                User = user;
                EndTime = endTime;
                CreateTime = createTime;
                Reason = reason;
            }
        }

        [DataLoad]
        private List<string> _quotes = new List<string>();

        [DataLoad, DataSave]
        private readonly List<ReminderData> _reminders = new List<ReminderData>();
        private bool _isReminderTimerRunning;

        private const string ColorRoleName = "ColorsAddRole";

        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("", group =>
            {
                group.CreateCommand("remind list")
                    .Description("Lists the reminders you have set.")
                    .Do(async e =>
                    {
                        List<ReminderData> userRem = _reminders.Where(r => r.User == e.User.Id).ToList();
                        if (!userRem.Any())
                        {
                            await e.Channel.SendMessage($"I have no reminders set for `{e.User.Name}`");
                            return;
                        }

                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine($"**Listing reminders for {e.User.Name}:**");

                        foreach (ReminderData rem in userRem)
                            builder.AppendLine(
                                $"`{rem.EndTime,-20}` Remaining time: `{(rem.EndTime - DateTime.Now).ToString(@"hh\:mm\:ss")}`");

                        await e.Channel.SendMessage(builder.ToString());
                    });

                group.CreateCommand("twink")
                    .MinPermissions((int) PermissionLevel.Trusted)
                    .Description("Moves Rebbit and Crixius to the Portuguese Twink Containment Zone TM (R) (c)")
                    .Do(async e =>
                    {
                        Channel channel = e.Server.GetChannel(Constants.TwinkChannelId);
                        await MoveToVoice(channel,
                            e.Server.GetUser(Constants.CrixiusId),
                            e.Server.GetUser(Constants.RebbitId));
                    });
                group.CreateCommand("remind")
                    .Description("Reminds you about something after the given time span has passed.")
                    .Parameter("timespan")
                    .Parameter("reason", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string rawSpan = e.GetArg("timespan");
                        _reminders.Add(new ReminderData(e.User.Id, TimeSpan.Parse(rawSpan), e.GetArg("reason")));
                        await e.Channel.SendMessage($"Reminding `{e.User.Name}` in `{rawSpan}`");
                    });
                group.CreateCommand("google")
                    .MinPermissions((int) PermissionLevel.User)
                    .Description("Lmgtfy")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage($"http://lmgtfy.com/?q={HttpUtility.UrlEncode(e.GetArg("query"))}");
                    });

                group.CreateCommand("quote")
                    .MinPermissions((int) PermissionLevel.User)
                    .Description("Kappa.")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage($"`{_quotes.PickRandom()}`");
                    });
                group.CreateCommand("qupte")
                    .Description("Ruby for fucks sake...")
                    .MinPermissions((int) PermissionLevel.User)
                    .Do(async e =>
                    {
                        const string quptePoolDir = Constants.DataFolderDir + @"12\";
                        if (!File.Exists(quptePoolDir)) return;

                        await e.Channel.SendFile(Directory.GetFiles(quptePoolDir).PickRandom());
                    });

            });

            manager.CreateCommands("color", group =>
            {
                group.CreateCommand("set")
                    .Description("Sets your username to a hex color. Format: RRGGBB")
                    .Parameter("hex")
                    .Do(async e =>
                    {
                        string stringhex = e.GetArg("hex").ToUpper();
                        Role role = e.Server.Roles.FirstOrDefault(x => x.Name == ColorRoleName + stringhex);

                        if (role == null || !e.User.HasRole(role) && role.CanEdit())
                        {
                            role = await e.Server.CreateRole(ColorRoleName + stringhex);
                            role.SetColor(stringhex);
                            await e.User.Edit(roles: GetOtherRoles(e.User).Concat(new[] { role }));
                        }
                        await CleanColorRoles(e.Server);
                    });
                group.CreateCommand("clear")
                    .Description("Removes your username color, returning it to default.")
                    .Do(async e =>
                    {
                        await e.User.Edit(roles: GetOtherRoles(e.User));
                    });

                group.CreateCommand("clean")
                    .MinPermissions((int)PermissionLevel.ServerModerator)
                    .Description("Removes unused color roles. Gets automatically called whenever a color is set.")
                    .Do(async e =>
                    {
                        await CleanColorRoles(e.Server);
                    });
            });

            if (!_isReminderTimerRunning)
            {
                Task.Run(() => StartReminderTimer());
                _isReminderTimerRunning = true;
            }
        }

        public void OnDataLoad()
        {
            
        }

        private async Task CleanColorRoles(Server server)
        {
            foreach (Role role in server.Roles
                .Where(role => role.Name.StartsWith(ColorRoleName))
                .Where(role => !role.Members.Any()))
            {
                await role.Delete();
            }
        }

        private IEnumerable<Role> GetOtherRoles(User user) => user.Roles.Where(x => !x.Name.StartsWith(ColorRoleName));

        private async void StartReminderTimer()
        {
            try
            {
                while (!_client.CancelToken.IsCancellationRequested)
                {
                    for (int i = _reminders.Count - 1; i >= 0; i--)
                    {
                        if (_reminders[i].EndTime > DateTime.Now) continue;
                        User user = _client.GetUser(_reminders[i].User);

                        if (user.PrivateChannel == null) await user.CreatePMChannel();
                        await user.PrivateChannel.SendMessage(
                            $"Paging you about the reminder you've set at `{_reminders[i].CreateTime}` for \"{_reminders[i].Reason}\"");
                        _reminders.RemoveAt(i);
                    }
                    await Task.Delay(1000); // wait 1 second.
                }
            }
            catch (TaskCanceledException)
            {
            } //expected
            Logger.Writeline("Stopped reminder timer");
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