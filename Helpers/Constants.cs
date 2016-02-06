namespace Stormbot.Helpers
{
    internal static class Constants
    {
        internal const string ConfigDir = DataFolderDir + "config.json";
#if DEBUG_DEV
        internal const string DataFolderDir = @"C:\Stuff\BotData\Dev\";
#else
        internal const string DataFolderDir = @"C:\Stuff\BotData\";
#endif
        internal const string TwitchEmoteFolderDir = DataFolderDir + @"emotes\";

       // internal const long RoleTrusted = 131446561547878400;
        internal const long UserOwner = 131467025456300032;

        internal const string RoleIdArg = "roleid";
        internal const string UserIdArg = "userid";
        internal const string ChannelIdArg = "channelid";

        internal static string Pass;

        internal static string TwitchUsername;
        internal static string TwitchOauth;

        internal static string FfmpegDir;
        internal static string LivestreamerDir;
        internal static string FfprobeDir;

        internal static int FfmpegProcessCount = 0;

        //  internal const ulong TwinkChannelId = 131490784544292865;
    }
}
