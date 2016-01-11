//https://github.com/RogueException/DiscordBot/blob/master/src/DiscordBot/Modules/Modules/ModulesModule.cs

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;

namespace Stormbot.Bot.Core.Modules
{
    public class ModulesModule : IDataModule
    {
        private ModuleService _moduleService;
        private DiscordClient _client;

        /// <summary>
        /// Stores the server id and the list of modules the server has enabled.
        /// </summary>
        [DataLoad] private Dictionary<ulong, HashSet<string>> _serverModulesDictionary =
            new Dictionary<ulong, HashSet<string>>();

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _moduleService = _client.Modules();

            manager.CreateCommands("module", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerModerator);

                group.CreateCommand("enable")
                    .Description("Enables a module on the current server.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = GetModule(e.GetArg("module"));
                        if (module == null)
                        {
                            await e.Channel.SendMessage("Unknown module");
                            return;
                        }
                        if (module.FilterType == ModuleFilter.None ||
                            module.FilterType == ModuleFilter.AlwaysAllowPrivate)
                        {
                            await e.Channel.SendMessage("This module is global and cannot be enabled/disabled.");
                            return;
                        }
                        if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        {
                            await e.Channel.SendMessage("This module doesn't support being enabled for servers.");
                            return;
                        }
                        var server = e.Server;
                        if (!module.EnableServer(server))
                        {
                            await
                                e.Channel.SendMessage(
                                    $"Module `{module.Id}` was already enabled for server `{server.Name}`.");
                            return;
                        }
                        AddModuleToSave(module.Id, e.Server.Id);
                        await e.Channel.SendMessage($"Module `{module.Id}` was enabled for server `{server.Name}`.");

                    });
                group.CreateCommand("disable")
                    .Description("Disables a module for the current server.")
                    .Parameter("module", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        ModuleManager module = GetModule(e.GetArg("module"));
                        if (module == null)
                        {
                            await e.Channel.SendMessage("Unknown module");
                            return;
                        }
                        if (module.FilterType == ModuleFilter.None ||
                            module.FilterType == ModuleFilter.AlwaysAllowPrivate)
                        {
                            await e.Channel.SendMessage("This module is global and cannot be enabled/disabled.");
                            return;
                        }
                        if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
                        {
                            await e.Channel.SendMessage("This module doesn't support being enabled for servers.");
                            return;
                        }
                        var server = e.Server;
                        if (!module.DisableServer(server))
                        {
                            await e.Channel.SendMessage($"Module `{module.Id}` was not enabled for server `{server.Name}`.");
                            return;
                        }
                        DeleteModuleFromSave(module.Id, e.Server.Id);
                        await e.Channel.SendMessage($"Module `{module.Id}` was disabled for server `{server.Name}`.");
                    });
                group.CreateCommand("list")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder("**Available modules:**\r\n");

                        foreach (
                            ModuleManager module in _moduleService.Modules)
                        {
                            builder.Append($"`* {module.Id,-20} ");

                            if (!(module.FilterType == ModuleFilter.None ||
                                module.FilterType == ModuleFilter.AlwaysAllowPrivate))
                                builder.AppendLine($"Enabled: {module.EnabledServers.Contains(e.Server)}`");
                            else
                                builder.AppendLine("Global`");
                        }

                        await e.Channel.SendMessage(builder.ToString());
                    });
            });
        }

        private void AddModuleToSave(string moduleId, ulong serverId)
        {
            if (_serverModulesDictionary.ContainsKey(serverId))
                _serverModulesDictionary[serverId].Add(moduleId);
            else
            {
                _serverModulesDictionary.Add(serverId, new HashSet<string> {moduleId});
            }
        }

        private void DeleteModuleFromSave(string moduleId, ulong serverId)
        {
            if (_serverModulesDictionary.ContainsKey(serverId))
                _serverModulesDictionary[serverId].Remove(moduleId);
        }

        public void OnDataLoad()
        {
            foreach (var pair in _serverModulesDictionary)
                foreach (ModuleManager module in pair.Value.Select(GetModule))
                    module?.EnableServer(_client.GetServer(pair.Key));
        }

        private ModuleManager GetModule(string id)
        {
            id = id.ToLowerInvariant();
            return _moduleService.Modules.FirstOrDefault(x => x.Id == id);
        }
    }
}