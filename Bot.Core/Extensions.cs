using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core
{
    public static class Extensions
    {
        public static AudioService Audio(this DiscordClient client, bool required = true)
            => client.Services.Get<AudioService>(required);

        public static CommandService Commands(this DiscordClient client, bool required = true)
            => client.Services.Get<CommandService>(required);

        public static bool CanRun(this ModuleManager manager, Channel channel)
            => manager.EnabledServers.Contains(channel.Server) || manager.EnabledChannels.Contains(channel);

        public static async Task SetColor(this Role role, uint hex)
        {
            if (!CanEdit(role)) return;
            await role.Edit(color: new Color(hex));
        }

        public static async Task SendPrivate(this User user, string message)
        {
            if (user.PrivateChannel == null)
                await user.CreatePMChannel();
            await user.SendMessage(message);
        }

        /// <summary>
        /// Returns a user found by it's userid, null if not found.
        /// </summary>
        public static User GetUser(this DiscordClient client, ulong userid)
            => client.Servers.Select(server => server.GetUser(userid)).FirstOrDefault(user => user != null);

        public static bool CanEdit(this Role role) => !role.IsEveryone || !role.IsManaged;

        /// <summary>
        /// Returns the role, which is found by the ulong paramater defined by Constants.RoleIdArg
        /// </summary>
        public static Role GetRole(this CommandEventArgs e)
            => e.Server.GetRole(ulong.Parse(e.GetArg(Constants.RoleIdArg)));

        /// <summary>
        /// Returns the user, which is found by the ulong paramater defined by Constants.UserIdArg
        /// </summary>
        public static User GetUser(this CommandEventArgs e)
            => e.Server.GetUser(ulong.Parse(e.GetArg(Constants.UserIdArg)));

        /// <summary>
        /// Returns the channel, which is found by the ulong paramater defined by Constants.ChannelIdArg
        /// </summary>
        public static Channel GetChannel(this CommandEventArgs e)
            => e.Server.GetChannel(ulong.Parse(e.GetArg(Constants.ChannelIdArg)));

        public static bool CanJoinChannel(this Channel voiceChannel, User user)
        {
            if (voiceChannel.Type != ChannelType.Voice) throw new ArgumentException(nameof(voiceChannel));
            return user.GetPermissions(voiceChannel).Connect;
        }

        /// <summary>
        /// Attempts to send a message to the given channel.
        /// </summary>
        /// <returns>Sent message if sent successfuly, null if we don't have permissions.</returns>
        public static async Task<Message> SafeSendMessage(this Channel textChannel, string msg)
        {
            if(textChannel.Type != ChannelType.Text) throw new ArgumentException(nameof(textChannel));

            if(textChannel.Server == null)
                return await textChannel.SendMessage(msg);

            if (!textChannel.Server.CurrentUser.GetPermissions(textChannel).SendMessages) return null;
           
            return await textChannel.SendMessage(msg);
        }

        public static async Task<Message> SafeSendFile(this Channel textChannel, string path)
        {
            if (textChannel.Type != ChannelType.Text) throw new ArgumentException(nameof(textChannel));
            if (!textChannel.Server.CurrentUser.GetPermissions(textChannel).AttachFiles) return null;

            return await textChannel.SendFile(path);
        }

        public static async Task SafeAddRoles(this User user, User caller, params Role[] roles)
        {
            if (caller.ServerPermissions.ManageRoles)
                await user.AddRoles(roles);
        }

        /// <summary>
        /// Attempts to join a voice channel.
        /// </summary>
        /// <param name="textCallback">Text callback channel to which we will write when we failed joining the audio channel.</param>
        /// <returns>Null if failed joining.</returns>
        public static async Task<IAudioClient> SafeJoin(this AudioService audio, Channel voiceChannel,
            Channel textCallback)
        {
            if (voiceChannel.Type != ChannelType.Voice) throw new ArgumentException(nameof(voiceChannel));
            if (textCallback.Type != ChannelType.Text) throw new ArgumentException(nameof(textCallback));

            if (voiceChannel.CanJoinChannel(voiceChannel.Server.CurrentUser))
            {
                try
                {
                    return await audio.Join(voiceChannel);
                }
                catch
                {
                    await textCallback.SafeSendMessage($"Failed joining voice channel `{voiceChannel.Name}`.");
                }
            }

            await textCallback.SafeSendMessage($"I don't have permission to join `{voiceChannel.Name}`.");
            return null;
        }
    }
}