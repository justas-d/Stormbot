using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.DynPerm
{
    [JsonObject]
    public class DynPermFullData
    {
        [JsonProperty]
        public string OriginalJson { get; }

        [JsonProperty]
        public DynamicPerms Perms { get; }

        [JsonProperty]
        public string PastebinUrl { get; set; }

        [JsonConstructor]
        private DynPermFullData(string originalJson, DynamicPerms perms, string pastebinUrl)
        {
            OriginalJson = originalJson;
            Perms = perms;
            PastebinUrl = pastebinUrl;
        }

        internal DynPermFullData(string originalJson, DynamicPerms perms) : this(originalJson, perms, null)
        {
        }
    }

    [JsonObject]
    public class DynamicPerms
    {
        [JsonProperty]
        public Dictionary<ulong, DynamicPermissionBlock> RolePerms { get; }

        [JsonProperty]
        public Dictionary<ulong, DynamicPermissionBlock> UserPerms { get; }

        [JsonConstructor]
        private DynamicPerms(Dictionary<ulong, DynamicPermissionBlock> roles,
            Dictionary<ulong, DynamicPermissionBlock> users)
        {
            if (roles == null)
                roles = new Dictionary<ulong, DynamicPermissionBlock>();

            if (users == null)
                users = new Dictionary<ulong, DynamicPermissionBlock>();

            RolePerms = roles;
            UserPerms = users;
        }
    }

    [JsonObject]
    public class DynamicPermissionBlock
    {
        [JsonProperty]
        public DynamicRestricionSet Allow { get; }

        [JsonProperty]
        public DynamicRestricionSet Deny { get; }

        [JsonConstructor]
        private DynamicPermissionBlock(ulong id, DynamicRestricionSet allow, DynamicRestricionSet deny)
        {
            if (allow == null)
                allow = new DynamicRestricionSet();

            if (deny == null)
                deny = new DynamicRestricionSet();

            Allow = allow;
            Deny = deny;
        }
    }

    [JsonObject]
    public class DynamicRestricionSet
    {
        [JsonProperty]
        public Dictionary<string, HashSet<ulong>> Modules { get; }

        [JsonProperty]
        public Dictionary<string, HashSet<ulong>> Commands { get; }

        [JsonConstructor]
        private DynamicRestricionSet(Dictionary<string, HashSet<ulong>> modules,
            Dictionary<string, HashSet<ulong>> commands)
        {
            if (modules == null)
                modules = new Dictionary<string, HashSet<ulong>>(StringComparer.InvariantCultureIgnoreCase);

            if (commands == null)
                commands = new Dictionary<string, HashSet<ulong>>(StringComparer.InvariantCultureIgnoreCase);


            if (!Equals(modules.Comparer, StringComparer.InvariantCultureIgnoreCase))
                modules = new Dictionary<string, HashSet<ulong>>(modules, StringComparer.InvariantCultureIgnoreCase);

            if (!Equals(commands.Comparer, StringComparer.InvariantCultureIgnoreCase))
                commands = new Dictionary<string, HashSet<ulong>>(commands, StringComparer.InvariantCultureIgnoreCase);

            Modules = modules;
            Commands = commands;
        }

        public DynamicRestricionSet() : this(null, null)
        {
        }
    }
}
