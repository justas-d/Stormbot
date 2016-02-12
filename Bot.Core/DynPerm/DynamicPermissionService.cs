using System.Collections.Concurrent;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionService : IService, IDataObject
    {
        private DiscordClient _client;

        [DataLoad, DataSave] private ConcurrentDictionary<ulong, DynamicPerms> _perms;

        private PermissionLevelService _defaultPerms;

        void IService.Install(DiscordClient client)
        {
            _client = client;
            _defaultPerms = client.Services.Get<PermissionLevelService>();
        }

        public bool TryAddOrUpdate(ulong server, string input)
        {
            DynamicPerms perms;
            try
            {
                perms = JsonConvert.DeserializeObject<DynamicPerms>(input);
            }
            catch (JsonException)
            {
                return false;
            }

            if (perms == null) return false;

            _perms.AddOrUpdate(server, perms, (k, v) => perms);
            return true;
        }

        public DynamicPerms GetPerms(ulong server)
        {
            DynamicPerms perms;
            _perms.TryGetValue(server, out perms);
            return perms;
        }

        public bool CanRunCommand(Command cmd, User user, Channel channel, PermissionLevel defaultPerms)
        {

            bool retval = false;
            DynamicPerms perms = _perms[channel.Server.Id];



            return retval;
        }

        void IDataObject.OnDataLoad()
        {
            if(_perms == null)
                _perms = new ConcurrentDictionary<ulong, DynamicPerms>();
        }
    }
}
