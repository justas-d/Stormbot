using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionService : IService, IDataObject
    {
        [DataLoad, DataSave] private ConcurrentDictionary<ulong, DynPermFullData> _perms;

        private DiscordClient _client;

        void IService.Install(DiscordClient client)
        {
            _client = client;
        }

        public void DestroyServerPerms(ulong server) => _perms.Remove(server);

        public DynPermFullData TryAddOrUpdate(ulong server, string input, out string error)
        {
            DynamicPerms perms;
            try
            {
                perms = JsonConvert.DeserializeObject<DynamicPerms>(input);
            }
            catch (JsonException ex)
            {
                error = $"Failed deserializing input. Make sure your input is valid JSON.\r\nDebug: JsonException: {ex.Message}";
                return null;
            }

            if (perms == null)
            {
                error = "Deserialization of input resulted in null params.";
                return null;
            }

            DynPermFullData fullPermData = new DynPermFullData(input, perms);

            // verify the data (role && user ids)
            foreach (DynamicPermissionBlock block in fullPermData.Perms.RolePerms)
            {
                if (_client.GetServer(server).GetRole(block.Id) == null)
                {
                    error = $"Role id {block.Id} not found.";
                    return null;
                }
            }

            foreach (DynamicPermissionBlock block in fullPermData.Perms.UserPerms)
            {
                if (_client.GetServer(server).GetUser(block.Id) == null)
                {
                    error = $"User id {block.Id} not found.";
                    return null;
                }
            }


            _perms.AddOrUpdate(server, fullPermData, (k, v) => fullPermData);

            error = null;
            return fullPermData;
        }

        public DynPermFullData GetPerms(ulong server)
        {
            DynPermFullData perms;
            _perms.TryGetValue(server, out perms);
            return perms;
        }

        void IDataObject.OnDataLoad()
        {
            if(_perms == null)
                _perms = new ConcurrentDictionary<ulong, DynPermFullData>();
        }
    }
}
