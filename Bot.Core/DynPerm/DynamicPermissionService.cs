using System.Collections.Concurrent;
using Discord;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionService : IService, IDataObject
    {
        [DataLoad, DataSave] private ConcurrentDictionary<ulong, DynPermFullData> _perms;


        void IService.Install(DiscordClient client)
        {
        }

        public void DestroyServerPerms(ulong server) => _perms.Remove(server);

        public DynPermFullData TryAddOrUpdate(ulong server, string input)
        {
            DynamicPerms perms;
            try
            {
                perms = JsonConvert.DeserializeObject<DynamicPerms>(input);
            }
            catch (JsonException)
            {
                return null;
            }

            if (perms == null) return null;
            DynPermFullData fullPermData = new DynPermFullData(input, perms);

            _perms.AddOrUpdate(server, fullPermData, (k, v) => fullPermData);
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
