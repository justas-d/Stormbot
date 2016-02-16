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

        public int DefaultPermissionLevel { get; }

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

            // if we do not have dynamic perms in place for the user's server, return the default perms.
            if (data == null || (!data.Perms.RolePerms.Any() && !data.Perms.UserPerms.Any()))
                return retval;

            /* 
              Firsly do role checks.
              Lower entries override higher entries. 
              To do that we have to iterate over the dict instead of using roles the user has as keys.
            */

            foreach (var pair in data.Perms.RolePerms)
            {
                if (user.HasRole(pair.Key))
                    retval = EvaluatePerms(pair.Value, command, retval, channel, ref error);
            }

            // users override roles, do them next.
            DynamicPermissionBlock permBlock;
            if (data.Perms.UserPerms.TryGetValue(user.Id, out permBlock))
                retval = EvaluatePerms(permBlock, command, retval, channel, ref error);

            return retval;
        }

        private bool EvaluatePerms(DynamicPermissionBlock dynPerms, Command command, bool canRunState, Channel channel,
            ref string error)
        {
            string category = command.Category.ToLowerInvariant();

            canRunState = EvalPermsExact(dynPerms.Allow.Modules, category, channel, canRunState, true, ref error);
            canRunState = EvalPermsExact(dynPerms.Deny.Modules, category, channel, canRunState, false, ref error);
            canRunState = EvalPermsCommands(dynPerms.Allow.Commands, command, channel, canRunState, true, ref error);
            canRunState = EvalPermsCommands(dynPerms.Deny.Commands, command, channel, canRunState, false, ref error);

            return canRunState;
        }

        private bool EvalPermsCommands(Dictionary<string, RestrictionData> dict, Command command, Channel channel,
            bool canRunState, bool setState, ref string error)
        {
            // check if full command exists in dict
            if (EvalPermsExact(dict, command.Text, channel, canRunState, setState, ref error) == setState)
                return setState;

            // look for group
            foreach (var pair in dict)
            {
                if (command.Text.StartsWith(pair.Key))
                    canRunState = EvalRestrictionData(pair.Value, channel, canRunState, setState, ref error);
            }

            return canRunState;
        }

        private bool EvalPermsExact(Dictionary<string, RestrictionData> dict, string query, Channel channel,
            bool canRunState, bool setState, ref string error)
        {
            RestrictionData restData;

            if (dict.TryGetValue(query, out restData))
                canRunState = EvalRestrictionData(restData, channel, canRunState, setState, ref error);

            return canRunState;
        }

        private bool EvalRestrictionData(RestrictionData restData, Channel channel, bool canRunState, bool setState,
            ref string error)
        {
            if (restData.ChannelRestrictions.Any())
            {
                if (restData.ChannelRestrictions.Contains(channel.Id) == setState)
                    canRunState = setState;
            }
            else
                canRunState = setState;

            if (restData.ErrorMessage != null)
                error = restData.ErrorMessage;

            return canRunState;
        }
    }
}
