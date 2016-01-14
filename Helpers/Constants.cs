// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

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

        internal const long RoleTrusted = 131446561547878400;
        internal const long UserOwner = 131467025456300032;

        internal const string RoleIdArg = "roleid";
        internal const string UserIdArg = "userid";
        internal const string ChannelIdArg = "channelid";

        internal const ulong RebbitId = 131468210531860480;
        internal const ulong CrixiusId = 131465844176715776;

        internal static string Pass;

        internal static string FfmpegDir;
        internal static string LivestreamerDir;
        internal static string FfprobeDir;

        internal const ulong TwinkChannelId = 131490784544292865;
    }
}
