namespace Stormbot.Bot.Core
{
    internal static class Constants
    {
#if DEBUG_DEV
        internal const string DataFolderDir = @"C:\Stuff\BotData\Dev\";
#else
        internal const string DataFolderDir = @"C:\Stuff\BotData\";
#endif
        internal const string CredentialsConfigDir = DataFolderDir + "credentials.json";
        internal const string CommonConfigDir = @"C:\Stuff\BotData\config-common.json";
        internal const string TwitchEmoteFolderDir = DataFolderDir + @"emotes\";

        internal const long UserOwner = 131467025456300032;

        internal const string RoleIdArg = "roleid";
        internal const string UserMentionArg = "usermention";
        internal const string ChannelIdArg = "channelid";
    }
}