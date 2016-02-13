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

        private int DefaultPermissionLevel { get; }

        public DynamicPermissionChecker(DiscordClient client, int defaultPerms)
        {
            DynPerms = client.Services.Get<DynamicPermissionService>();
            DefaultPermChecker = client.Services.Get<PermissionLevelService>();
            DefaultPermissionLevel = defaultPerms;
        }

        public bool CanRun(Command command, User user, Channel channel, out string error)
        {
            DynPermFullData data = DynPerms.GetPerms(channel.Server.Id);
            error = null;

            // apply default perms.
            bool retval = DefaultPermissionLevel <= DefaultPermChecker.GetPermissionLevel(user, channel);

            if (data == null || (!data.Perms.RolePerms.Any() && !data.Perms.UserPerms.Any()))
                return retval;
                    // if we do not have dynamic perms in place for the user's server, return the default perms.

            // firsly do role checks.
            foreach (ulong roleId in user.Roles.Select(r => r.Id))
            {
                foreach (DynamicPermissionBlock dynPerms in data.Perms.RolePerms.Where(rp => rp.Id == roleId))
                {
                    retval = EvaluatePerms(dynPerms, command, retval);
                }
            }

            // users override roles, do them next.
            foreach (DynamicPermissionBlock dynPerms in data.Perms.UserPerms.Where(up => up.Id == user.Id))
            {
                retval = EvaluatePerms(dynPerms, command, retval);
            }

            return retval;
        }

        private bool EvaluatePerms(DynamicPermissionBlock dynPerms, Command command, bool canRunState)
        {
            if (dynPerms.Allow.Modules.Contains(command.Category))
                canRunState = true;
            if (dynPerms.Deny.Modules.Contains(command.Category))
                canRunState = false;

            if (dynPerms.Allow.Commands.Contains(command.Text) ||
                HashsetContainsComamndGroup(dynPerms.Allow.Commands, command.Text))
                canRunState = true;
            if (dynPerms.Deny.Commands.Contains(command.Text) ||
                HashsetContainsComamndGroup(dynPerms.Deny.Commands, command.Text))
                canRunState = false;

            return canRunState;
        }

        private bool HashsetContainsComamndGroup(HashSet<string> hashset, string commandText)
            => hashset.Any(commandText.StartsWith);
    }
}
