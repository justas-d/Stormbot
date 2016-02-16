using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;
using RestSharp.Extensions.MonoHttp;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    public class QualityOfLifeModule : IDataObject, IModule
    {
        [JsonObject]
        private class ReminderData
        {
            [JsonProperty]
            public ulong User { get; }

            [JsonProperty]
            public DateTime EndTime { get; }

            [JsonProperty]
            public DateTime CreateTime { get; }

            [JsonProperty]
            public string Reason { get; }

            [JsonConstructor]
            private ReminderData(ulong user, DateTime endTime, DateTime createTime, string reason)
            {
                User = user;
                EndTime = endTime;
                CreateTime = createTime;
                Reason = reason;
            }

            public ReminderData(ulong user, TimeSpan span, string reason)
            {
                User = user;
                EndTime = DateTime.Now + span;
                CreateTime = DateTime.Now;
                Reason = reason;
            }
        }

        private const byte MinQuoteSize = 3;
        private const string ColorRoleName = "ColorsAddRole";
        private DiscordClient _client;
        private bool _isReminderTimerRunning;
        [DataLoad, DataSave] private ConcurrentDictionary<ulong, HashSet<string>> _quoteDictionary;
        [DataLoad, DataSave] private List<ReminderData> _reminders = new List<ReminderData>();

        void IDataObject.OnDataLoad()
        {
            if (_quoteDictionary == null)
                _quoteDictionary = new ConcurrentDictionary<ulong, HashSet<string>>();
        }

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateDynCommands("", PermissionLevel.User, group =>
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

                        if (input.Length < MinQuoteSize)
                        {
                            await e.Channel.SafeSendMessage($"Quote too short. (min {MinQuoteSize})");
                            return;
                        }

                        if (string.IsNullOrEmpty(input))
                        {
                            await e.Channel.SafeSendMessage("Input cannot be empty.");
                            return;
                        }
                        GetQuotes(e.Server).Add(input);
                        await e.Channel.SafeSendMessage("Added quote.");
                    });

                group.CreateCommand("coin")
                    .Description("Flips a coin.")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder("**Coin flip**: ");
                        builder.Append(StaticRandom.Bool() ? "Heads!" : "Tails!");
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });
            });

            manager.CreateDynCommands("color", PermissionLevel.User, group =>
            {
                // make sure we have permission to manage roles
                group.AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles);

                group.CreateCommand("set")
                    .Description("Sets your username to a hex color. Format: RRGGBB")
                    .Parameter("hex")
                    .Do(async e =>
                    {
                        string stringhex = e.GetArg("hex");
                        uint hex;

                        if (!DiscordUtils.ToHex(stringhex, out hex))
                        {
                            await e.Channel.SendMessage("Failed parsing input. Valid format: `RRGGBB`");
                            return;
                        }

                        Role role = e.Server.Roles.FirstOrDefault(x => x.Name == ColorRoleName + stringhex);

                        if (role == null || !e.User.HasRole(role))
                        {
                            role = await e.Server.CreateRole(ColorRoleName + stringhex);
                            if (!await role.SafeEdit(color: new Color(hex)))
                                await
                                    e.Channel.SendMessage(
                                        $"Failed editing role. Make sure it's not everyone or managed.");
                        }
                        await e.User.AddRoles(role);

                        await CleanColorRoles(e.Server);
                    });
                group.CreateCommand("clear")
                    .Description("Removes your username color, returning it to default.")
                    .Do(async e =>
                    {
                        foreach (Role role in e.User.Roles.Where(role => role.Name.StartsWith(ColorRoleName)))
                            await e.User.RemoveRoles(role);
                    });

                group.CreateCommand("clean")
                    .MinDynPermissions((int) PermissionLevel.ServerModerator)
                    .Description("Removes unused color roles. Gets automatically called whenever a color is set.")
                    .Do(async e => await CleanColorRoles(e.Server));
            });

            if (!_isReminderTimerRunning)
            {
                Task.Run(() => StartReminderTimer());
                _isReminderTimerRunning = true;
            }
        }

        private HashSet<string> GetQuotes(Server server)
        {
            if (!_quoteDictionary.ContainsKey(server.Id))
                _quoteDictionary.TryAdd(server.Id, new HashSet<string>());

            return _quoteDictionary[server.Id];
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
                    await Task.Delay(5000);
                }
            }
            catch (TaskCanceledException)
            {
            } //expected
            Logger.Writeline("Stopped reminder timer");
        }
    }
}