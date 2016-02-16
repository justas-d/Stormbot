using System;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace Stormbot.Bot.Core.DynPerm
{
    public static class DynPermExtenstions
    {
        public static DiscordClient UsingDynamicPerms(this DiscordClient client)
        {
            client.Services.Add(new DynamicPermissionService());
            client.AddModule<DynamicPermissionModule>("Dynamic Permissions");
            return client;
        }

        public static CommandBuilder MinDynPermissions(this CommandBuilder builder, int deafultPerms)
        {
            builder.AddCheck(new DynamicPermissionChecker(builder.Service.Client, deafultPerms));
            return builder;
        }

        public static CommandGroupBuilder MinDynPermissions(this CommandGroupBuilder builder, int deafultPerms)
        {
            builder.AddCheck(new DynamicPermissionChecker(builder.Service.Client, deafultPerms));
            return builder;
        }

        public static CommandService MinDynPermissions(this CommandService service, int deafultPerms)
        {
            service.Root.AddCheck(new DynamicPermissionChecker(service.Client, deafultPerms));
            return service;
        }

        public static void CreateDynCommands(this ModuleManager manager, string prefix,
            PermissionLevel defaultPermissionsLevel, Action<CommandGroupBuilder> builder)
        {
            CommandService commandService = manager.Client.Services.Get<CommandService>();
            commandService.CreateGroup(prefix, x =>
            {
                x.Category(manager.Name);
                x.AddCheck(new ModuleChecker(manager));
                x.MinDynPermissions((int) defaultPermissionsLevel);
                builder(x);
            });
        }
    }
}
