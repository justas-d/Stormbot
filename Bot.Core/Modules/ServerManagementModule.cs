using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Stormbot.Bot.Core.DynPerm;

namespace Stormbot.Bot.Core.Modules
{
    public class ServerManagementModule : IModule
    {
        void IModule.Install(ModuleManager manager)
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

                group.CreateCommand("prune")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageMessages ||
                                                  chnl.Server.CurrentUser.GetPermissions(chnl).ManageMessages)
                    .Description("Deletes the last 100 messages sent in this channel.")
                    .Do(async e =>
                    {
                        foreach (Message msg in await e.Channel.DownloadMessages())
                        {
                            if (msg != null)
                                await msg.Delete();
                        }
                    });

                group.CreateCommand("topic")
                    .Description("Sets the topic of a channel, found by its id.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        Channel channel = e.GetChannel();
                        string topic = e.GetArg("topic");

                        if (channel == null)
                        {
                            await e.Channel.SendMessage($"Channel not found.");
                            return;
                        }
                        if (!await channel.SafeEditChannel(topic: topic))
                        {
                            await
                                e.Channel.SendMessage($"I don't have sufficient permissions to edit {channel.Mention}");
                            return;
                        }

                        await e.Channel.SendMessage($"Set the topic of {channel.Mention} to `{topic}`.");
                    });

                group.CreateCommand("name")
                    .Description("Sets the name of a channel, found by its id.")
                    .Parameter(Constants.ChannelIdArg)
                    .Parameter("name")
                    .Do(async e =>
                    {
                        Channel channel = e.GetChannel();
                        string name = e.GetArg("name");

                        if (channel == null)
                        {
                            await e.Channel.SendMessage($"Channel not found.");
                            return;
                        }
                        string nameBefore = channel.Name;

                        if (!await channel.SafeEditChannel(name))
                        {
                            await
                                e.Channel.SendMessage($"I don't have sufficient permissions to edit {channel.Mention}");
                            return;
                        }

                        await e.Channel.SendMessage($"Set the name of `{nameBefore}` to `{name}`.");
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

                // commands, which can only be called if the bot user has rights to manager roles.
                group.CreateGroup("", manageRolesGroup =>
                {
                    manageRolesGroup.AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles);

                    manageRolesGroup.CreateCommand("add")
                        .Description("Adds a role with the given name if it doesn't exist.")
                        .Parameter("name", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            string name = e.GetArg("name");
                            Role role = FindRole(name, e);
                            if (role != null)
                            {
                                await e.Channel.SendMessage("Role not found.");
                                return;
                            }

                            await e.Server.CreateRole(name);
                            await e.Channel.SafeSendMessage($"Created role `{name}`");
                        });
                    manageRolesGroup.CreateCommand("rem")
                        .Description("Removes a role with the given id if it exists.")
                        .Parameter(Constants.RoleIdArg)
                        .Do(async e =>
                        {
                            Role role = e.GetRole();

                            if (role == null)
                            {
                                await e.Channel.SendMessage("Role not found.");
                                return;
                            }

                            if (role.IsEveryone || role.IsHoisted || role.IsManaged)
                            {
                                await e.Channel.SendMessage("You cannot remove this role.");
                                return;
                            }

                            await role.Delete();
                            await e.Channel.SafeSendMessage($"Removed role `{role.Name}`");
                        });

                    manageRolesGroup.CreateCommand("edit perm")
                        .Description("Edits permissions for a given role, found by id, if it exists.")
                        .Parameter(Constants.RoleIdArg)
                        .Parameter("permission")
                        .Parameter("value")
                        .Do(async e =>
                        {
                            Role role = e.GetRole();

                            if (role == null)
                            {
                                await e.Channel.SendMessage("Role not found.");
                                return;
                            }

                            ServerPermissions edited = role.Permissions;
                            PropertyInfo prop = edited.GetType().GetProperty(e.GetArg("permission"));

                            bool value = bool.Parse(e.GetArg("value"));
                            prop.SetValue(edited, value);

                            await role.SafeEdit(perm: edited);
                            await
                                e.Channel.SafeSendMessage(
                                    $"Set permission `{prop.Name}` in `{role.Name}` to `{value}`");
                        });

                    manageRolesGroup.CreateCommand("edit color")
                        .Description("Edits the color (RRGGBB) for a given role, found by id, if it exists. ")
                        .Parameter(Constants.RoleIdArg)
                        .Parameter("hex")
                        .Do(async e =>
                        {
                            Role role = e.GetRole();

                            if (role == null)
                            {
                                await e.Channel.SendMessage("Role not found.");
                                return;
                            }

                            uint hex;

                            if (!DiscordUtils.ToHex(e.GetArg("hex"), out hex))
                                return;

                            if (!await role.SafeEdit(color: new Color(hex)))
                                await
                                    e.Channel.SendMessage(
                                        $"Failed editing role. Make sure it's not everyone or managed.");
                        });
                });

                group.CreateCommand("listperm")
                    .Description("Lists the permissions for a given roleid")
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        Role role = e.GetRole();

                        if (role == null)
                        {
                            await e.Channel.SendMessage("Role not found.");
                            return;
                        }

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
                    .Parameter(Constants.UserMentionArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();

                        if (user == null)
                        {
                            await e.Channel.SendMessage("User not found.");
                            return;
                        }

                        await user.Edit(bool.Parse(e.GetArg("val")));
                        await e.Channel.SafeSendMessage($"Muted `{user.Name}`");
                    });
                group.CreateCommand("deaf")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.DeafenMembers)
                    .Description("Deafens(true)/Undeafens(false) @user")
                    .Parameter(Constants.UserMentionArg)
                    .Parameter("val")
                    .Do(async e =>
                    {
                        User user = e.GetUser();

                        if (user == null)
                        {
                            await e.Channel.SendMessage("User not found.");
                            return;
                        }

                        await user.Edit(isDeafened: bool.Parse(e.GetArg("val")));
                        await e.Channel.SafeSendMessage($"Deafened `{user.Name}`");
                    });
                group.CreateCommand("move")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.MoveMembers)
                    .Description("Moves a @user to a given voice channel")
                    .Parameter(Constants.UserMentionArg)
                    .Parameter("channelid", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        User user = e.GetUser();

                        if (user == null)
                        {
                            await e.Channel.SendMessage("User not found.");
                            return;
                        }

                        Channel moveChnl = e.Server.GetChannel(ulong.Parse(e.GetArg("channelid")));
                        await user.Edit(voiceChannel: moveChnl);
                        await e.Channel.SafeSendMessage($"Moved `{user.Name}` to `{moveChnl.Name}`");
                    });
                group.CreateCommand("role add")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.ManageRoles)
                    .Description("Adds a role, found by id, to @user if they dont have it.")
                    .Parameter(Constants.UserMentionArg)
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Role role = e.GetRole();

                        if (user == null)
                        {
                            await e.Channel.SendMessage("User not found.");
                            return;
                        }

                        if (role == null)
                        {
                            await e.Channel.SendMessage("Role not found.");
                            return;
                        }

                        if (!user.HasRole(role))
                        {
                            await user.Edit(roles: user.Roles.Concat(new[] {role}));
                            await e.Channel.SafeSendMessage($"Given role `{role.Name}` to `{user.Mention}`");
                        }
                    });

                group.CreateCommand("role list")
                    .Description("Returns a list of roles a @user has.")
                    .Parameter(Constants.UserMentionArg)
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
                    .Description("Removes a roleid from a @user if they have it.")
                    .Parameter(Constants.UserMentionArg)
                    .Parameter(Constants.RoleIdArg)
                    .Do(async e =>
                    {
                        User user = e.GetUser();
                        Role role = e.GetRole();

                        if (user == null)
                        {
                            await e.Channel.SendMessage("User not found.");
                            return;
                        }

                        if (role == null)
                        {
                            await e.Channel.SendMessage("Role not found.");
                            return;
                        }

                        if (user.HasRole(role))
                        {
                            await user.RemoveRoles(role);
                            await e.Channel.SafeSendMessage($"Removed role `{role.Name}` from `{user.Name}`.");
                        }
                    });

                group.CreateCommand("kick")
                    .Description("Kicks a @user.")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.KickMembers)
                    .Parameter("userMention")
                    .Do(async e =>
                    {
                        User user = e.Server.GetUser(DiscordUtils.ParseMention(e.GetArg("userMention")).First());
                        if (user == null)
                        {
                            await e.Channel.SendMessage($"User not found.");
                            return;
                        }
                        string userName = user.Name; // in case user is disposed right after we kick them.

                        await user.Kick();
                        await e.Channel.SendMessage($"Kicked {userName}.");
                    });

                group.CreateCommand("ban")
                    .Description("Bans an @user. Also allows for message pruning for a given amount of days.")
                    .AddCheck((cmd, usr, chnl) => chnl.Server.CurrentUser.ServerPermissions.BanMembers)
                    .Parameter("userMention")
                    .Parameter("pruneDays", ParameterType.Optional)
                    .Do(async e =>
                    {
                        User user = e.Server.GetUser(DiscordUtils.ParseMention(e.GetArg("userMention")).First());
                        int pruneDays = 0;
                        string pruneRaw = e.GetArg("pruneDays");

                        if (!string.IsNullOrEmpty(pruneRaw))
                            pruneDays = int.Parse(pruneRaw);

                        if (user == null)
                        {
                            await e.Channel.SendMessage($"User not found.");
                            return;
                        }
                        string userName = user.Name; // in case user is disposed right after we kick them.

                        await e.Server.Ban(user, pruneDays);
                        await e.Channel.SendMessage($"Banned {userName}");
                    });
            });
        }

        private void CreateNameIdList(StringBuilder builder, IEnumerable<dynamic> values)
        {
            foreach (dynamic val in values) builder.AppendLine($"* `{val.Name,-30} {val.Id,-20}`");
        }

        private Role FindRole(string name, CommandEventArgs e)
            => e.Server.Roles.FirstOrDefault(x => x.Name == name);
    }
}