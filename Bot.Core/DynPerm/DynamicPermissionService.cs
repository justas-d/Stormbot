using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public DynPermFullData TryAddOrUpdate(ulong serverId, string input, out string error)
        {
            try
            {
                DynamicPerms perms = JsonConvert.DeserializeObject<DynamicPerms>(input);

                if (perms == null)
                {
                    error = "Deserialization of input resulted in null params.";
                    return null;
                }

                DynPermFullData fullPermData = new DynPermFullData(input, perms);
                Server server = _client.GetServer(serverId);

                // verify the data (role && user ids)
                if (server == null)
                {
                    error = $"Server id {serverId} not found.";
                    return null;
                }

                foreach (var pair in fullPermData.Perms.RolePerms)
                {
                    ulong invalidChannelId;
                    if (IsInvalidChannelsInBlock(pair.Value, server, out invalidChannelId))
                    {
                        error = $"Channel id {invalidChannelId} not found.";
                        return null;
                    }
                }

                foreach (var pair in fullPermData.Perms.UserPerms)
                {
                    if (server.GetUser(pair.Key) == null)
                    {
                        error = $"User id {pair.Key} not found.";
                        return null;
                    }
                }

                // data looks fine, add the dynrole to the dict.
                _perms.AddOrUpdate(serverId, fullPermData, (k, v) => fullPermData);

                error = null;
                return fullPermData;
            }
            catch (Exception ex)
            {
                error =
                    $"Failed deserializing input. Make sure your input is valid JSON.\r\nDebug: ```{ex.GetType().Name}: {ex.Message}:{ex.TargetSite}```";
            }

            return null;
        }

        private bool IsInvalidChannelsInBlock(DynamicPermissionBlock block, Server server, out ulong invalidId)
            => IsInvalidChannelsInSet(block.Allow, server, out invalidId) ||
               IsInvalidChannelsInSet(block.Deny, server, out invalidId);

        private bool IsInvalidChannelsInSet(DynamicRestricionSet set, Server server, out ulong invalidId)
            => IsInvalidChannelsInDict(set.Commands, server, out invalidId) ||
               IsInvalidChannelsInDict(set.Modules, server, out invalidId);

        private bool IsInvalidChannelsInDict(Dictionary<string, HashSet<ulong>> dict, Server server, out ulong invalidId)
        {
            foreach (var pair in dict)
            {
                foreach (ulong channelId in pair.Value)
                {
                    if (server.GetChannel(channelId) == null)
                    {
                        invalidId = channelId;
                        return true;
                    }
                }
            }

            invalidId = ulong.MinValue;
            return false;
        }

        public DynPermFullData GetPerms(ulong server)
        {
            DynPermFullData perms;
            _perms.TryGetValue(server, out perms);
            return perms;
        }

        void IDataObject.OnDataLoad()
        {
            if (_perms == null)
                _perms = new ConcurrentDictionary<ulong, DynPermFullData>();
        }
    }
}
