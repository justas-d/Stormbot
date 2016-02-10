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

        public static async Task SetColor(this Role role, string stringhex)
        {
            if (!CanEdit(role)) return;
            if (stringhex.Length > 6) return; //input is invalid if length isn't 0 < x > 7
            uint hex = uint.Parse(stringhex, NumberStyles.HexNumber);
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
    }
}