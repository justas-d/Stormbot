using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

namespace Stormbot.Bot.Core.DynPerm
{
    public static class DynPermExtenstions
    {
        public static DynamicPermissionService DynPerms(this DiscordClient client, bool required = true)
            => client.Services.Get<DynamicPermissionService>(required);

        public static DiscordClient UsingDynamicPerms(this DiscordClient client)
        {
            client.Services.Add(new DynamicPermissionService());
            client.AddModule<DynamicPermissionModule>("Dynamic Permissions");
            return client;
        }

        public static CommandBuilder MinPermissions(this CommandBuilder builder, int deafultPerms)
        {
            builder.AddCheck(new DynamicPermissionChecker(builder.Service.Client, deafultPerms));
            return builder;
        }
        public static CommandGroupBuilder MinPermissions(this CommandGroupBuilder builder, int deafultPerms)
        {
            builder.AddCheck(new DynamicPermissionChecker(builder.Service.Client, deafultPerms));
            return builder;
        }
        public static CommandService MinPermissions(this CommandService service, int deafultPerms)
        {
            service.Root.AddCheck(new DynamicPermissionChecker(service.Client, deafultPerms));
            return service;
        }
    }
}
