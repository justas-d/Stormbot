using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core
{
    public static class DiscordUtils
    {
        private static readonly Regex MentionIdRegex = new Regex(@"(\@|\#)([0-9]+?)\>");

        public static IEnumerable<ulong> ParseMention(string input)
        {
            // cant figure out how to make the regex ignore those chars so uhh i guess replace them?
            foreach (var match in MentionIdRegex.Matches(input))
                yield return ulong.Parse(match.ToString().Replace("@", "").Replace("#", "").Replace(">", ""));
        }

        // quick hack
        public static bool ToHex(string input, out uint outVal)
        {
            input = input.ToUpperInvariant();

            if (input.Length > 6)
            {
                outVal = 0;
                return false;
            }

            outVal = uint.Parse(input, NumberStyles.HexNumber);
            return true;
        }

        public static async Task JoinInvite(string inviteId, Channel callback)
        {
            Invite invite = await callback.Client.GetInvite(inviteId);
            if (invite == null)
            {
                await callback.SafeSendMessage("Invite not found.");
                return;
            }
            if (invite.IsRevoked)
            {
                await
                    callback.SafeSendMessage("This invite has expired or the bot is banned from that server.");
                return;
            }

            await invite.Accept();
            await callback.SafeSendMessage("Joined server.");
            await Constants.Owner.SendPrivate($"Joined server: `{invite.Server.Name}`.");
        }
    }
}
