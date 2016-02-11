using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class ModulesModule : IDataModule
    {
        private ModuleService _moduleService;
        private DiscordClient _client;

        /// <summary>
        /// Stores the server id and the list of modules the server has enabled.
        /// </summary>
        [DataLoad, DataSave]
        private ConcurrentDictionary<ulong, HashSet<string>> _serverModulesDictionary =
            new ConcurrentDictionary<ulong, HashSet<string>>();

        /// <summary>
        /// Stores the channel id and the list of modules it has enabled.
        /// </summary>
        [DataLoad, DataSave]
        private ConcurrentDictionary<ulong, HashSet<string>> _channelModulesDictionary =
            new ConcurrentDictionary<ulong, HashSet<string>>();

        private HashSet<string> DefaultModules => new HashSet<string>
        {
            "Bot",
            "QoL",
            "Information",
            "Annoucements",
            "Execute"
        };

        private HashSet<string> PrivateModules => new HashSet<string>()
        {
            "Audio",
            "Personal"
        };

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _moduleService = _client.Modules();

            manager.CreateCommands("module", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerModerator);

                group.CreateCommand("channel enable")
                    .Description("Enables a module on the current channel.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = await VerifyFindModule(e.GetArg("module"), e.Channel);
                        if (module == null) return;

                        if (!module.FilterType.HasFlag(ModuleFilter.ChannelWhitelist))
                        {
                            await e.Channel.SafeSendMessage("This module doesn't support being enabled for channel.");
                            return;
                        }

                        if (PrivateModules.FirstOrDefault(m => m.ToLowerInvariant() == module.Id) != null &&
                            e.User.Id != Constants.UserOwner)
                        {
                            await
                                e.Channel.SafeSendMessage("This module is private. Use }contact for more information.");
                            return;
                        }

                        Channel channel = e.Channel;

                        if (!module.EnableChannel(channel))
                        {
                            await
                                e.Channel.SafeSendMessage(
                                    $"Module `{module.Id}` was already enabled for channel `{channel.Name}`.");
                            return;
                        }
                        _channelModulesDictionary.AddModuleToSave(module.Id, e.Channel.Id);
                        await e.Channel.SafeSendMessage($"Module `{module.Id}` was enabled for channel `{channel.Name}`.");
                    });

                group.CreateCommand("channel disable")
                    .Description("Disable a module on the current channel.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = await VerifyFindModule(e.GetArg("module"), e.Channel);
                        if (module == null) return;

                        if (!module.FilterType.HasFlag(ModuleFilter.ChannelWhitelist))
                        {
                            await e.Channel.SafeSendMessage("This module doesn't support being enabled for channel.");
                            return;
                        }

                        Channel channel = e.Channel;

                        if (!module.DisableChannel(channel))
                        {
                            await
                                e.Channel.SafeSendMessage(
                                    $"Module `{module.Id}` was not enabled for channel `{channel.Name}`.");
                            return;
                        }
                        _channelModulesDictionary.DeleteModuleFromSave(module.Id, e.Channel.Id);
                        await e.Channel.SafeSendMessage($"Module `{module.Id}` was disabled for channel `{channel.Name}`.");
                    });

                group.CreateCommand("server enable")
                    .Description("Enables a module on the current server.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = await VerifyFindModule(e.GetArg("module"), e.Channel);
                        if (module == null) return;

                        if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        {
                            await e.Channel.SafeSendMessage("This module doesn't support being enabled for servers.");
                            return;
                        }

                        if (PrivateModules.FirstOrDefault(m => m.ToLowerInvariant() == module.Id) != null &&
                            e.User.Id != Constants.UserOwner)
                        {
                            await
                                e.Channel.SafeSendMessage("This module is private. Use }contact for more information.");
                            return;
                        }

                        Server server = e.Server;

                        if (!module.EnableServer(server))
                        {
                            await
                                e.Channel.SafeSendMessage(
                                    $"Module `{module.Id}` was already enabled for server `{server.Name}`.");
                            return;
                        }
                        _serverModulesDictionary.AddModuleToSave(module.Id, e.Server.Id);
                        await e.Channel.SafeSendMessage($"Module `{module.Id}` was enabled for server `{server.Name}`.");

                    });
                group.CreateCommand("server disable")
                    .Description("Disables a module for the current server.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = await VerifyFindModule(e.GetArg("module"), e.Channel);
                        if (module == null) return;

                        if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        {
                            await e.Channel.SafeSendMessage("This module doesn't support being enabled for servers.");
                            return;
                        }

                        Server server = e.Server;

                        if (!module.DisableServer(server))
                        {
                            await
                                e.Channel.SafeSendMessage(
                                    $"Module `{module.Id}` was not enabled for server `{server.Name}`.");
                            return;
                        }
                        _serverModulesDictionary.DeleteModuleFromSave(module.Id, e.Server.Id);
                        await e.Channel.SafeSendMessage($"Module `{module.Id}` was disabled for server `{server.Name}`.");
                    });
                group.CreateCommand("list")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder("**Available modules:**\r\n");

                        foreach (ModuleManager module in _moduleService.Modules)
                        {
                            builder.Append($"`* {module.Id,-20} ");

                            if (module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                                builder.Append($"Globally server: {module.EnabledServers.Contains(e.Server), -5} ");
                            if (module.FilterType.HasFlag(ModuleFilter.ChannelWhitelist))
                                builder.Append($"Channel: {module.EnabledChannels.Contains(e.Channel), -5}");
                            builder.AppendLine("`");
                        }
                        
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });
            });

            _client.JoinedServer += async (s, e) =>
            {
                foreach (string moduleName in DefaultModules)
                {
                    ModuleManager module = await VerifyFindModule(moduleName, null, false);
                    if (module == null) return;

                    if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        throw new InvalidOperationException();

                    Server server = e.Server;

                    module.EnableServer(server);
                    _serverModulesDictionary.AddModuleToSave(module.Id, server.Id);
                }
            };
        }

        private async Task<ModuleManager> VerifyFindModule(string id, Channel callback, bool useCallback = true)
        {
            ModuleManager module = GetModule(id);
            if (module == null)
            {
                if (useCallback)
                    await callback.SafeSendMessage("Unknown module");
                return null;
            }
            if (module.FilterType == ModuleFilter.None ||
                module.FilterType == ModuleFilter.AlwaysAllowPrivate)
            {
                if (useCallback)
                    await callback.SafeSendMessage("This module is global and cannot be enabled/disabled.");
                return null;
            }
            return module;
        }

        void IDataModule.OnDataLoad()
        {
            foreach (KeyValuePair<ulong, HashSet<string>> pair in _serverModulesDictionary)
                foreach (ModuleManager module in pair.Value.Select(GetModule))
                    if (module != null && module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        module.EnableServer(_client.GetServer(pair.Key));

            foreach (KeyValuePair<ulong, HashSet<string>> pair in _channelModulesDictionary)
                foreach (ModuleManager module in pair.Value.Select(GetModule))
                    if (module != null && module.FilterType.HasFlag(ModuleFilter.ChannelWhitelist))
                        module.EnableChannel(_client.GetChannel(pair.Key));
        }

        private ModuleManager GetModule(string id)
        {
            id = id.ToLowerInvariant();
            return _moduleService.Modules.FirstOrDefault(x => x.Id == id);
        }
    }

    // tfw no nested class extension methods
    internal static class PrivateExtenstions
    {
        internal static void AddModuleToSave(this ConcurrentDictionary<ulong, HashSet<string>> dict, string moduleId, ulong serverId)
        {
            if (dict.ContainsKey(serverId))
                dict[serverId].Add(moduleId);
            else
                dict.TryAdd(serverId, new HashSet<string> { moduleId });
        }

        internal static void DeleteModuleFromSave(this ConcurrentDictionary<ulong, HashSet<string>> dict, string moduleId, ulong serverId)
        {
            if (dict.ContainsKey(serverId))
                dict[serverId].Remove(moduleId);
        }
    }
}