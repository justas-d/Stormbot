using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class ServerManagementModule : IModule
    {
        public void Install(ModuleManager manager)
        {
            manager.CreateDynCommands("channel", PermissionLevel.ServerAdmin, group =>
            {
                group.CreateCommand("list text")
                    .Description("Lists all text channels in server")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing text channels in server:**");
                        CreateNameIdList(builder, e.Server.TextChannels);
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });

                group.CreateCommand("list voice")
                    .Description("Lists all voice channels in server")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing voice channels in server:**");
                        CreateNameIdList(builder, e.Server.VoiceChannels);
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });

                group.CreateCommand("desc")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageChannels ||
                                                  chnl.Server.CurrentUser.GetPermissions(chnl).ManageChannel)
                    .Description("Sets the channel description.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("desc", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.GetChannel().Edit(null, e.GetArg("desc"));
                    });
                group.CreateCommand("name")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageChannels ||
                                                  chnl.Server.CurrentUser.GetPermissions(chnl).ManageChannel)
                    .Description("Sets the channel name.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("value", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.GetChannel().Edit(e.GetArg("value"));
                    });
            });

            manager.CreateDynCommands("role", PermissionLevel.ServerAdmin, group =>
            {
                group.CreateCommand("list")
                    .Description("Lists all the roles the server has")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing roles in server:**");
                        CreateNameIdList(builder, e.Server.Roles);
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });
                group.CreateCommand("add")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Adds a role with the given name if it doesn't exist.")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string name = e.GetArg("name");
                        Role role = FindRole(name, e);
                        if (role != null) return;

                        await e.Server.CreateRole(name);
                        await e.Channel.SafeSendMessage($"Created role `{name}`");
                    });
                group.CreateCommand("rem")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Removes a role with the given id if it exists.")
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        Role role = e.GetRole();
                        if (role == null || role.IsManaged || role.IsEveryone) return;

                        await role.Delete();
                        await e.Channel.SafeSendMessage($"Removed role `{role.Name}`");
                    });

                group.CreateCommand("listperm")
                    .Description("Lists the permissions for a given roleid")
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        Role role = e.GetRole();
                        ServerPermissions perms = role.Permissions;
                        await
                            e.Channel.SafeSendMessage($"**Listing permissions for {role.Name}**\r\n:" +
                                                      $"{"CreateInstantInvite",-25}: {perms.CreateInstantInvite}\r\n" +
                                                      $"{"KickMembers",-25}: {perms.KickMembers}\r\n" +
                                                      $"{"BanMembers",-25}: {perms.BanMembers}\r\n" +
                                                      $"{"ManageRoles",-25}: {perms.ManageRoles}\r\n" +
                                                      $"{"ManageChannels",-25}: {perms.ManageChannels}\r\n" +
                                                      $"{"ManageServer",-25}: {perms.ManageServer}\r\n" +
                                                      $"{"ReadMessages",-25}: {perms.ReadMessages}\r\n" +
                                                      $"{"SafeSendMessages",-25}: {perms.SendMessages}\r\n" +
                                                      $"{"SendTTSMessages",-25}: {perms.SendTTSMessages}\r\n" +
                                                      $"{"ManageMessages",-25}: {perms.ManageMessages}\r\n" +
                                                      $"{"EmbedLinks",-25}: {perms.EmbedLinks}\r\n" +
                                                      $"{"AttachFiles",-25}: {perms.AttachFiles}\r\n" +
                                                      $"{"ReadMessageHistory",-25}: {perms.ReadMessageHistory}\r\n" +
                                                      $"{"MentionEveryone",-25}: {perms.MentionEveryone}\r\n" +
                                                      $"{"Connect",-25}: {perms.Connect}\r\n" +
                                                      $"{"Speak",-25}: {perms.Speak}\r\n" +
                                                      $"{"MuteMembers",-25}: {perms.MuteMembers}\r\n" +
                                                      $"{"DeafenMembers",-25}: {perms.DeafenMembers}\r\n" +
                                                      $"{"MoveMembers",-25}: {perms.MoveMembers}\r\n" +
                                                      $"{"UseVoiceActivation",-25}: {perms.UseVoiceActivation}`"
                                );
                    });

                group.CreateCommand("edit perm")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Edits permissions for a given role, found by id, if it exists.")
                    .Parameter(Constants.RoleIdArg)
                    .Parameter("permission")
                    .Parameter("value")
                    .Do(async e =>
                    {
                        Role role = e.GetRole();
                        ServerPermissions edited = role.Permissions;
                        PropertyInfo prop = edited.GetType().GetProperty(e.GetArg("permission"));

                        bool value = bool.Parse(e.GetArg("value"));
                        prop.SetValue(edited, value);

                        await role.Edit(permissions: edited);
                        await
                            e.Channel.SafeSendMessage(
                                $"Set permission `{prop.Name}` in `{role.Name}` to `{value}`");

                    });

                group.CreateCommand("edit color")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Edits the color (RRGGBB) for a given role, found by id, if it exists. ")
                    .Parameter(Constants.RoleIdArg)
                    .Parameter("hex")
                    .Do(async e =>
                    {
                        uint hex;

                        if (!DiscordUtils.ToHex(e.GetArg("hex"), out hex))
                            return;

                        await e.GetRole().SetColor(hex);
                    });
            });

            manager.CreateDynCommands("ued", PermissionLevel.ServerModerator, group =>
            {
                group.CreateCommand("list")
                    .Description("Lists users in server and their UIDs")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing users in server:**");
                        CreateNameIdList(builder, e.Server.Users);
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });

                group.CreateCommand("mute")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.MuteMembers)
                    .Description("Mutes(true)/unmutes(false) the userid")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        await user.Edit(bool.Parse(e.GetArg("val")));
                        await e.Channel.SafeSendMessage($"Muted `{user.Name}`");
                    });
                group.CreateCommand("deaf")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.DeafenMembers)
                    .Description("Deafens(true)/Undeafens(false) the current edit user.")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        await user.Edit(isDeafened: bool.Parse(e.GetArg("val")));
                        await e.Channel.SafeSendMessage($"Deafened `{user.Name}`");
                    });
                group.CreateCommand("move")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.MoveMembers)
                    .Description("Moves the current edit user to a given voice channel")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("channelid", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Channel moveChnl = e.Server.GetChannel(ulong.Parse(e.GetArg("channelid")));
                        await user.Edit(voiceChannel: moveChnl);
                        await e.Channel.SafeSendMessage($"Moved `{user.Name}` to `{moveChnl.Name}`");
                    });
                group.CreateCommand("role add")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Adds a role, found by id, to the current edit user if it doesn't already have it.")
                    .Parameter(Constants.UserIdArg)
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Role role = e.GetRole();

                        if (!user.HasRole(role) && role.CanEdit())
                        {
                            await user.Edit(roles: user.Roles.Concat(new[] {role}));
                            await e.Channel.SafeSendMessage($"Given role `{role.Name}` to `{user.Name}`");
                        }
                    });

                group.CreateCommand("role list")
                    .Description("Returns a list of roles the given userid has.")
                    .Parameter(Constants.UserIdArg)
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        User user = e.GetUser();
                        builder.AppendLine($"**Listing roles for {user.Name}:**");
                        CreateNameIdList(builder, e.Server.Roles);
                        await e.Channel.SafeSendMessage(builder.ToString());
                    });
                group.CreateCommand("role rem")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Removes a roleid from the userid if it has it.")
                    .Parameter(Constants.UserIdArg)
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Role role = e.GetRole();
                        if (user.HasRole(role) && role.CanEdit())
                        {
                            await user.RemoveRoles(role);
                            await e.Channel.SafeSendMessage($"Removed role `{role.Name}` from `{user.Name}`.");
                        }
                    });
            });
        }

        private void CreateNameIdList(StringBuilder builder, IEnumerable<dynamic> values)
        {
            foreach (dynamic val in values) builder.AppendLine($"* `{val.Name,-30} {val.Id,-20}`");
        }

        private Role FindRole(string name, CommandEventArgs e) => e.Server.Roles.FirstOrDefault(x => x.Name == name);
    }
}
