// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules
{
    public class ServerManagementModule : IModule
    {
        public void Install(ModuleManager manager)
        {
            manager.CreateCommands("channel", group =>
            {
                group.MinPermissions((int)PermissionLevel.ServerAdmin);

                group.CreateCommand("list text")
                    .Description("Lists all text channels in server")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing text channels in server:**");
                        CreateNameIdList(builder, e.Server.TextChannels);
                        await e.Channel.SendMessage(builder.ToString());
                    });

                group.CreateCommand("list voice")
                    .Description("Lists all voice channels in server")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing voice channels in server:**");
                        CreateNameIdList(builder, e.Server.VoiceChannels);
                        await e.Channel.SendMessage(builder.ToString());
                    });

                group.CreateCommand("desc")
                    .Description("Sets the channel description.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("desc", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.GetChannel().Edit(null, e.GetArg("desc"));
                    });
                group.CreateCommand("name")
                    .Description("Sets the channel name.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("value")
                    .Do(async e =>
                    {
                        await e.GetChannel().Edit(e.GetArg("value"));
                    });
            });

            manager.CreateCommands("role", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerAdmin);

                group.CreateCommand("list")
                    .Description("Lists all the roles the server has")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing roles server:**");
                        CreateNameIdList(builder, e.Server.Roles);
                        await e.Channel.SendMessage(builder.ToString());
                    });
                group.CreateCommand("add")
                    .Description("Adds a role with the given name if it doesn't exist.")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string name = e.GetArg("name");
                        Role role = FindRole(name, e);
                        if (role != null) return;

                        await e.Server.CreateRole(name);
                        await e.Channel.SendMessage($"Created role `{name}`");
                    });
                group.CreateCommand("rem")
                    .Description("Removes a role with the given id if it exists.")
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        Role role = e.GetRole();
                        if (role == null || role.IsManaged || role.IsEveryone) return;

                        await role.Delete();
                        await e.Channel.SendMessage($"Removed role `{role.Name}`");
                    });

                group.CreateCommand("listperm")
                    .Description("Lists the permissions for a given roleid")
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        Role role = e.GetRole();
                        ServerPermissions perms = role.Permissions;
                        await e.Channel.SendMessage($"**Listing permissions for `{role.Name}`**{Environment.NewLine}` +" +
                                                    $"{"CreateInstantInvite",-25}: {perms.CreateInstantInvite}{Environment.NewLine}" +
                                                    $"{"KickMembers",-25}: {perms.KickMembers}{Environment.NewLine}" +
                                                    $"{"BanMembers",-25}: {perms.BanMembers}{Environment.NewLine}" +
                                                    $"{"ManageRoles",-25}: {perms.ManageRoles}{Environment.NewLine}" +
                                                    $"{"ManageChannels",-25}: {perms.ManageChannels}{Environment.NewLine}" +
                                                    $"{"ManageServer",-25}: {perms.ManageServer}{Environment.NewLine}" +
                                                    $"{"ReadMessages",-25}: {perms.ReadMessages}{Environment.NewLine}" +
                                                    $"{"SendMessages",-25}: {perms.SendMessages}{Environment.NewLine}" +
                                                    $"{"SendTTSMessages",-25}: {perms.SendTTSMessages}{Environment.NewLine}" +
                                                    $"{"ManageMessages",-25}: {perms.ManageMessages}{Environment.NewLine}" +
                                                    $"{"EmbedLinks",-25}: {perms.EmbedLinks}{Environment.NewLine}" +
                                                    $"{"AttachFiles",-25}: {perms.AttachFiles}{Environment.NewLine}" +
                                                    $"{"ReadMessageHistory",-25}: {perms.ReadMessageHistory}{Environment.NewLine}" +
                                                    $"{"MentionEveryone",-25}: {perms.MentionEveryone}{Environment.NewLine}" +
                                                    $"{"Connect",-25}: {perms.Connect}{Environment.NewLine}" +
                                                    $"{"Speak",-25}: {perms.Speak}{Environment.NewLine}" +
                                                    $"{"MuteMembers",-25}: {perms.MuteMembers}{Environment.NewLine}" +
                                                    $"{"DeafenMembers",-25}: {perms.DeafenMembers}{Environment.NewLine}" +
                                                    $"{"MoveMembers",-25}: {perms.MoveMembers}{Environment.NewLine}" +
                                                    $"{"UseVoiceActivation",-25}: {perms.UseVoiceActivation}`"
                            );

                    });

                group.CreateCommand("edit perm")
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
                            e.Channel.SendMessage(
                                $"Set permission `{prop.Name}` in `{role.Name}` to `{value}`");

                    });
                group.CreateCommand("edit color")
                    .Description("Edits the color (RRGGBB) for a given role, found by id, if it exists. ")
                    .Parameter(Constants.RoleIdArg)
                    .Parameter("hex")
                    .Do(e =>
                    {
                        e.GetRole().SetColor(e.GetArg("hex"));
                    });
            });

            manager.CreateCommands("ued", group =>
            {
                group.MinPermissions((int) PermissionLevel.ServerModerator);

                group.CreateCommand("list")
                    .Description("Lists users in server and their UIDs")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Listing users in server:**");
                        CreateNameIdList(builder, e.Server.Users);
                        await e.Channel.SendMessage(builder.ToString());
                    });

                group.CreateCommand("mute")
                    .Description("Mutes(true)/unmutes(false) the userid")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        await user.Edit(bool.Parse(e.GetArg("val")));
                        await e.Channel.SendMessage($"Muted `{user.Name}`");
                    });
                group.CreateCommand("deaf")
                    .Description("Deafens(true)/Undeafens(false) the current edit user.")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        await user.Edit(isDeafened: bool.Parse(e.GetArg("val")));
                        await e.Channel.SendMessage($"Deafened `{user.Name}`");
                    });
                group.CreateCommand("move")
                    .Description("Moves the current edit user to a given voice channel")
                    .Parameter(Constants.UserIdArg)
                    .Parameter("channelid", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Channel moveChnl = e.Server.GetChannel(ulong.Parse(e.GetArg("channelid")));
                        await user.Edit(voiceChannel: moveChnl);
                        await e.Channel.SendMessage($"Moved `{user.Name}` to `{moveChnl.Name}`");
                    });
                group.CreateCommand("role add")
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
                            await e.Channel.SendMessage($"Given role `{role.Name}` to `{user.Name}`");
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
                        await e.Channel.SendMessage(builder.ToString());
                    });
                group.CreateCommand("role rem")
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
                            await e.Channel.SendMessage($"Removed role `{role.Name}` from `{user.Name}`.");
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