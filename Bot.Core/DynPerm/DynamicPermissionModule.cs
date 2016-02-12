using Discord;
using Discord.Commands;
using Discord.Modules;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionModule : IModule
    {
        private DiscordClient _client;
        private DynamicPermissionService _dynPerms;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _dynPerms = _client.DynPerms();

            manager.CreateCommands("dynperm", group =>
            {
                group.CreateCommand("set")
                    .Parameter("perms", ParameterType.Unparsed)
                    .Do(e =>
                    {
                        _dynPerms.TryAddOrUpdate(e.Server.Id, e.GetArg("perms"));
                    });
            });
        }
    }
}
