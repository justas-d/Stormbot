using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using Discord.Commands.Permissions.Levels;
using StrmyCore;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionChecker : IPermissionChecker
    {
        private DynamicPermissionService DynPerms { get; }
        private PermissionLevelService DefaultPermChecker { get; }

        private int DefaultPermissionLevel { get; }

        public DynamicPermissionChecker(DiscordClient client, int defaultPerms)
        {
            DynPerms = client.Services.Get<DynamicPermissionService>();
            DefaultPermChecker = client.Services.Get<PermissionLevelService>();
            DefaultPermissionLevel = defaultPerms;
        }

        public bool CanRun(Command command, User user, Channel channel, out string error)
        {
            error = null;

            if (channel.IsPrivate)
                return DefaultPermissionLevel <= DefaultPermChecker.GetPermissionLevel(user, channel);

            DynPermFullData data = DynPerms.GetPerms(channel.Server.Id);

            // apply default perms.
            bool retval = DefaultPermissionLevel <= DefaultPermChecker.GetPermissionLevel(user, channel);

            if (data == null || (!data.Perms.RolePerms.Any() && !data.Perms.UserPerms.Any()))
                return retval;
            // if we do not have dynamic perms in place for the user's server, return the default perms.

            /* 
              Firsly do role checks.
              Lower entries override higher entries. 
              To do that we have to iterate over the dict instead of using roles the user has as keys.
            */

            foreach (var pair in data.Perms.RolePerms)
            {
                if (user.HasRole(pair.Key))
                    retval = EvaluatePerms(pair.Value, command, retval, channel);
            }

            // users override roles, do them next.
            DynamicPermissionBlock permBlock;
            if (data.Perms.UserPerms.TryGetValue(user.Id, out permBlock))
                retval = EvaluatePerms(permBlock, command, retval, channel);

            return retval;
        }

        private bool EvaluatePerms(DynamicPermissionBlock dynPerms, Command command, bool canRunState, Channel channel)
        {
            string category = command.Category.ToLowerInvariant();

            canRunState = EvalPermsExact(dynPerms.Allow.Modules, category, channel, canRunState, true);
            canRunState = EvalPermsExact(dynPerms.Deny.Modules, category, channel, canRunState, false);
            canRunState = EvalPermsCommands(dynPerms.Allow.Commands, command, channel, canRunState, true);
            canRunState = EvalPermsCommands(dynPerms.Deny.Commands, command, channel, canRunState, false);

            return canRunState;
        }

        private bool EvalPermsCommands(Dictionary<string, HashSet<ulong>> dict, Command command, Channel channel,
            bool canRunState, bool setState)
        {
            // check if full command exists in dict
            if (EvalPermsExact(dict, command.Text, channel, canRunState, setState) == setState)
                return setState;

            // look for group
            foreach (var pair in dict)
            {
                if (command.Text.StartsWith(pair.Key))
                {
                    if (pair.Value.Any())
                    {
                        if (pair.Value.Contains(channel.Id))
                            canRunState = setState;
                    }
                    else
                        canRunState = setState;
                }
            }

            return canRunState;
        }

        private bool EvalPermsExact(Dictionary<string, HashSet<ulong>> dict, string query, Channel channel,
            bool canRunState, bool setState)
        {
            HashSet<ulong> whenInChannelList;

            if (dict.TryGetValue(query, out whenInChannelList))
            {
                if (whenInChannelList.Any())
                {
                    if (whenInChannelList.Contains(channel.Id))
                        canRunState = setState;
                }
                else
                    canRunState = setState;
            }
            return canRunState;
        }
    }
}
