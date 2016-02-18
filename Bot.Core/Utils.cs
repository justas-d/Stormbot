using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using Discord.Commands.Permissions.Levels;
using Stormbot.Bot.Core.DynPerm;
using StrmyCore;

namespace Stormbot.Bot.Core
{
    public static class DiscordUtils
    {
        private static readonly object _commandsMdLock = new object();

        private static readonly Regex MentionIdRegex = new Regex(@"(\@|\#)([0-9]+?)\>");

        public static async Task<bool> CanJoinAndTalkInVoiceChannel(Channel voiceChannel, Channel callback)
        {
            if (voiceChannel.Type != ChannelType.Voice) throw new ArgumentException(nameof(voiceChannel));
            if (callback.Type != ChannelType.Text) throw new ArgumentException(nameof(callback));

            if (!voiceChannel.Server.CurrentUser.GetPermissions(voiceChannel).Speak)
            {
                await callback.SafeSendMessage($"I don't have permission to speak in `{voiceChannel.Name}`.");
                return false;
            }
            if (!voiceChannel.CanJoinChannel(voiceChannel.Server.CurrentUser))
            {
                await callback.SafeSendMessage($"I don't have permission to join `{voiceChannel.Name}`");
                return false;
            }

            return true;
        }

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
            await Config.Owner.SendPrivate($"Joined server: `{invite.Server.Name}`.");
        }

        public static Task GenerateCommandMarkdown(DiscordClient client)
            => Task.Run(() =>
            {
                lock (_commandsMdLock)
                {
                    if (string.IsNullOrEmpty(Config.CommandsMdDir))
                    {
                        Logger.FormattedWrite("CommandMd",
                            "Skipping command.md auto generation. Config.CommandsMdDir was not set.");
                        return;
                    }

                    string tableStart =
                        $"Commands | Parameters | Description | Default Permissions | Supports DynPerms? {Environment.NewLine}--- | --- | --- | --- | ---";

                    StringBuilder builder = new StringBuilder()
                        .AppendLine("# StormBot command table.")
                        .AppendLine($"This file was automatically generated at {DateTime.UtcNow} UTC.\r\n\r\n")
                        .AppendLine("### Preface")
                        .AppendLine(
                            "This document contains every command, that has been registered in the CommandService system, their paramaters, their desciptions and their default permissions.")
                        .AppendLine(
                            "Every command belongs to a cetain module. These modules can be enabled and disabled at will using the Modules module. Each comamnd is seperated into their parent modules command table.")
                        .AppendLine($"{Environment.NewLine}{Environment.NewLine}")
                        .AppendLine(
                            $"Each and every one of these commands can be triggered by saying `{client.Commands().Config.PrefixChar}<command>` or `@<BotName> <command>`")
                        .AppendLine($"{Environment.NewLine}## Commands");

                    string currentModule = null;
                    foreach (Command cmd in client.Commands().AllCommands)
                    {
                        if (cmd.Text == "help") continue;
                        if (cmd.Category == "Personal") continue;

                        if (currentModule != cmd.Category)
                        {
                            currentModule = cmd.Category;
                            builder.AppendLine($"{Environment.NewLine}#### {currentModule}");
                            builder.AppendLine(tableStart);
                        }

                        builder.Append($"`{cmd.Text}` ");

                        if (cmd.Aliases.Any())
                        {
                            builder.Append("*Aliases*: ");

                            foreach (string alias in cmd.Aliases)
                                builder.Append($"`{alias}` ");

                            builder.Append(" ");
                        }

                        builder.Append("|");

                        foreach (CommandParameter param in cmd.Parameters)
                        {
                            switch (param.Type)
                            {
                                case ParameterType.Required:
                                    builder.Append($" `<{param.Name}>`");
                                    break;
                                case ParameterType.Optional:
                                    builder.Append($" `[{param.Name}]`");
                                    break;
                                case ParameterType.Multiple:
                                    builder.Append($" `[{param.Name}...]`");
                                    break;
                                case ParameterType.Unparsed:
                                    builder.Append(" `[-]`");
                                    break;
                            }
                        }

                        builder.Append($" | {cmd.Description} | ");

                        // perms are a bit of a hack seeing as _checks is private.
                        IPermissionChecker[] checkers = (IPermissionChecker[])
                            cmd.GetType()
                                .GetRuntimeFields()
                                .FirstOrDefault(f => f.Name == "_checks")
                                .GetValue(cmd);

                        // get max value of PermissionLevel
                        PermissionLevel permLevel =
                            Enum.GetValues(typeof (PermissionLevel)).Cast<PermissionLevel>().Max();
                        bool supportsDynPerms = false;

                        foreach (IPermissionChecker permCheck in checkers)
                        {
                            PermissionLevelChecker perms = permCheck as PermissionLevelChecker;
                            if (perms != null)
                                permLevel = (PermissionLevel) perms.MinPermissions;

                            DynamicPermissionChecker dynPerms = permCheck as DynamicPermissionChecker;
                            if (dynPerms != null)
                            {
                                supportsDynPerms = true;
                                permLevel = (PermissionLevel) dynPerms.DefaultPermissionLevel;
                            }
                        }

                        builder.Append($"{permLevel} | ");
                        builder.Append(supportsDynPerms ? "✓" : "-");
                        builder.Append(Environment.NewLine);
                    }

                    Logger.FormattedWrite("CommandMd", "Generated commands.md", ConsoleColor.Green);
                    File.WriteAllText(Config.CommandsMdDir, builder.ToString());
                }
            });
    }
}