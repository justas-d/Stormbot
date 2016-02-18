using System;
using System.Globalization;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace Stormbot.Bot.Core.DynPerm
{
    public static class DynPermExtenstions
    {
        // we need to define these outside of the call to Activator seeing as, if we do, the compiler will use the `Type type, params object[] args` overload and not the one we want.
        private static BindingFlags _moduleCheckerActivatorFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                                   BindingFlags.Instance;

        public static DiscordClient UsingDynamicPerms(this DiscordClient client)
        {
            client.AddService(new DynamicPermissionService());
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
            CommandService commandService = manager.Client.GetService<CommandService>();
            commandService.CreateGroup(prefix, x =>
            {
                x.Category(manager.Name);

                x.AddCheck((ModuleChecker)
                    Activator.CreateInstance(typeof (ModuleChecker), _moduleCheckerActivatorFlags, null,
                        new object[] {manager}, null));

                x.MinDynPermissions((int) defaultPermissionsLevel);
                builder(x);
            });
        }
    }
}