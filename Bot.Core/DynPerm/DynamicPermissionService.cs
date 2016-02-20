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
        private DiscordClient _client;
        [DataLoad, DataSave] private ConcurrentDictionary<ulong, DynPermFullData> _perms;

        void IDataObject.OnDataLoad()
        {
            if (_perms == null)
                _perms = new ConcurrentDictionary<ulong, DynPermFullData>();
        }

        void IService.Install(DiscordClient client)
        {
            _client = client;
        }

        public void DestroyServerPerms(ulong server)
            => _perms.Remove(server);

        public DynPermFullData SetDynPermFullData(ulong serverId, string input, out string error)
        {
            try
            {
                DynamicPerms perms = JsonConvert.DeserializeObject<DynamicPerms>(input);

                if (perms == null)
                {
                    error = "Deserialization of input resulted in null params.";
                    return null;
                }

                DynPermFullData fullPermData = new DynPermFullData(perms);
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

        private bool IsInvalidChannelsInDict(Dictionary<string, RestrictionData> dict, Server server,
            out ulong invalidId)
        {
            foreach (var pair in dict)
            {
                foreach (ulong channelId in pair.Value.ChannelRestrictions)
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
            DynPermFullData data;
            _perms.TryGetValue(server, out data);
            return data;
        }

        public DynPermFullData GetOrAddPerms(Server server)
            => _perms.GetOrAdd(server.Id, key => new DynPermFullData());
    }
}