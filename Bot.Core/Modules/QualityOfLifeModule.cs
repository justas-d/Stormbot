using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        [DataLoad, DataSave] private ConcurrentDictionary<ulong, List<string>> _quoteDictionary;

        [DataLoad, DataSave] private List<ReminderData> _reminders = new List<ReminderData>();

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
                            await e.Channel.SafeSendMessage($"I have no reminders set for `{e.User.Name}`");
                            return;
                        }

                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine($"**Listing reminders for {e.User.Name}:**");

                        foreach (ReminderData rem in userRem)
                            builder.AppendLine(
                                $"`{rem.EndTime,-20}` Remaining time: `{(rem.EndTime - DateTime.Now).ToString(@"hh\:mm\:ss")}`");

                        await e.Channel.SafeSendMessage(builder.ToString());
                    });

                // todo : move twink to personal module
                //group.CreateCommand("twink")
                //    .MinPermissions((int) PermissionLevel.Trusted)
                //    .Description("Moves Rebbit and Crixius to the Portuguese Twink Containment Zone TM (R) (c)")
                //    .Do(async e =>
                //    {
                //        Channel channel = e.Server.GetChannel(Constants.TwinkChannelId);
                //        await MoveToVoice(channel,
                //            e.Server.GetUser(Constants.CrixiusId),
                //            e.Server.GetUser(Constants.RebbitId));
                //    });

                group.CreateCommand("remind")
                    .Description("Reminds you about something after the given time span has passed.")
                    .Parameter("timespan")
                    .Parameter("reason", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string rawSpan = e.GetArg("timespan");
                        _reminders.Add(new ReminderData(e.User.Id, TimeSpan.Parse(rawSpan), e.GetArg("reason")));
                        await e.Channel.SafeSendMessage($"Reminding `{e.User.Name}` in `{rawSpan}`");
                    });
                group.CreateCommand("google")
                    .Description("Lmgtfy")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string query = e.GetArg("query");
                        if (string.IsNullOrEmpty(query)) return;
                        await e.Channel.SafeSendMessage($"http://lmgtfy.com/?q={HttpUtility.UrlEncode(query)}");
                    });

                group.CreateCommand("quote")
                    .Description("Prints a quote out from your servers' quote list.")
                    .Do(async e =>
                    {
                        string quote = GetQuotes(e.Server).PickRandom();
                        if (quote == null)
                        {
                            await e.Channel.SafeSendMessage($"Server quote list is empty.");
                            return;
                        }
                        await e.Channel.SafeSendMessage($"`{quote}`");
                    });

                group.CreateCommand("addquote")
                    .Description("Adds a quote to your servers' quote list.")
                    .Parameter("quote", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string input = e.GetArg("quote");
                        if (string.IsNullOrEmpty(input))
                        {
                            await e.Channel.SafeSendMessage("Input cannot be empty.");
                            return;
                        }
                        GetQuotes(e.Server).Add(input);
                        await e.Channel.SafeSendMessage("Added quote.");
                    });

                // todo : move qupte to personal module
                //group.CreateCommand("qupte")
                //    .Description("Ruby for fucks sake...")
                //    .MinPermissions((int) PermissionLevel.User)
                //    .Do(async e =>
                //    {
                //        const string quptePoolDir = Constants.DataFolderDir + @"12\";
                //        if (!Directory.Exists(quptePoolDir)) return;

                //        await e.Channel.SendFile(Directory.GetFiles(quptePoolDir).PickRandom());
                //    });
            });

            manager.CreateCommands("color", group =>
            {
                // make sure we have permission to manage roles
                group.AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles);

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
                            await role.SetColor(stringhex);
                            await e.User.Edit(roles: GetOtherRoles(e.User).Concat(new[] {role}));
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
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Description("Removes unused color roles. Gets automatically called whenever a color is set.")
                    .Do(async e => await CleanColorRoles(e.Server));
            });

            if (!_isReminderTimerRunning)
            {
                Task.Run(() => StartReminderTimer());
                _isReminderTimerRunning = true;
            }
        }

        private List<string> GetQuotes(Server server)
        {
            if (!_quoteDictionary.ContainsKey(server.Id))
                _quoteDictionary.TryAdd(server.Id, new List<string>());

            return _quoteDictionary[server.Id];
        }

        public void OnDataLoad()
        {
            if (_quoteDictionary == null)
                _quoteDictionary = new ConcurrentDictionary<ulong, List<string>>();
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
                        await user.PrivateChannel.SafeSendMessage(
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
