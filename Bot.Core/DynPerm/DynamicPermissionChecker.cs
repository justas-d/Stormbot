using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using Discord.Commands.Permissions.Levels;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionChecker : IPermissionChecker
    {
        private DynamicPermissionService DynPerms { get; }
        private PermissionLevelService DefaultPermChecker { get; }

        private static readonly ConcurrentDictionary<string, IEnumerable<string>> CachedCommandGroups =
            new ConcurrentDictionary<string, IEnumerable<string>>();

        private int DefaultPermissionLevel { get; }

        public DynamicPermissionChecker(DiscordClient client, int defaultPerms)
        {
            DynPerms = client.Services.Get<DynamicPermissionService>();
            DefaultPermChecker = client.Services.Get<PermissionLevelService>();
            DefaultPermissionLevel = defaultPerms;
        }

        public bool CanRun(Command command, User user, Channel channel, out string error)
        {
            DynamicPerms perms = DynPerms.GetPerms(channel.Server.Id);
            error = null;

            if (perms == null || (!perms.RolePerms.Any() && !perms.UserPerms.Any()))
            {
                // apply default perms if we do not have dynamic perms in place for the user's server.
                return DefaultPermissionLevel <= DefaultPermChecker.GetPermissionLevel(user, channel);
            }

            bool retval = true;

            // firsly do role checks.
            foreach (ulong roleId in user.Roles.Select(r => r.Id))
            {
                foreach (DynamicPermissionBlock dynPerms in perms.RolePerms.Where(rp => rp.Id == roleId))
                    retval = EvaluatePerms(dynPerms, command);
            }

            // users override roles, do them next.
            foreach (DynamicPermissionBlock dynPerms in perms.UserPerms)
                retval = EvaluatePerms(dynPerms, command);

            return retval;
        }

        private bool EvaluatePerms(DynamicPermissionBlock dynPerms, Command command)
        {
            bool retval = true;

            if (dynPerms.Allow.Modules.Contains(command.Category))
                retval = true;
            if (dynPerms.Deny.Modules.Contains(command.Category))
                retval = false;

            if (dynPerms.Allow.Commands.Contains(command.Text) ||
                HashsetContainsComamndGroup(dynPerms.Allow.Commands, command.Text))
                retval = true;
            if (dynPerms.Deny.Commands.Contains(command.Text) ||
                HashsetContainsComamndGroup(dynPerms.Deny.Commands, command.Text))
                retval = false;

            return retval;
        }

        private bool HashsetContainsComamndGroup(HashSet<string> hashset, string commandText)
        {
            if (CachedCommandGroups.ContainsKey(commandText))
                return hashset.Intersect(CachedCommandGroups[commandText]).Any();

            string[] split = commandText.Split(' ');
            CachedCommandGroups.TryAdd(commandText, split);
            return hashset.Intersect(split).Any();
        }
    }
}
