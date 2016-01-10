// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

namespace Stormbot.Bot.Core.Modules
{
    /// <summary> Creates a role for each built-in color and allows users to freely select them. </summary>
    internal class ColorsModule : IModule
    {
        private string _colorRoleName = "ColorsAddRole";

        public void Install(ModuleManager manager)
        {
            manager.CreateCommands("color", group =>
            {
                group.CreateCommand("set")
                    .Description("Sets your username to a hex color. Format: RRGGBB")
                    .Parameter("hex")
                    .Do(async e =>
                    {
                        string stringhex = e.GetArg("hex").ToUpper();
                        Role role = e.Server.Roles.FirstOrDefault(x => x.Name == _colorRoleName + stringhex);

                        if (role == null || !e.User.HasRole(role) && role.CanEdit())
                        {
                            role = await e.Server.CreateRole(_colorRoleName + stringhex);
                            role.SetColor(stringhex);
                            await e.User.Edit(roles: GetOtherRoles(e.User).Concat(new[] {role}));
                        }
                        await CleanColorRoles(e.Server);
                    });
                group.CreateCommand("clear")
                    .Description("Removes your username color, returning it to default.")
                    .Do(async e =>
                    {
                        await e.User.Edit(roles: GetOtherRoles(e.User));
                    });

                group.CreateCommand("clean")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Description("Removes unused color roles. Gets automatically called whenever a color is set.")
                    .Do(async e =>
                    {
                        await CleanColorRoles(e.Server);
                    });
            });
        }

        private async Task CleanColorRoles(Server server)
        {
            foreach (Role role in server.Roles
                .Where(role => role.Name.StartsWith(_colorRoleName))
                .Where(role => !role.Members.Any()))
            {
                await role.Delete();
            }
        }

        private IEnumerable<Role> GetOtherRoles(User user) => user.Roles.Where(x => !x.Name.StartsWith(_colorRoleName));
    }
}