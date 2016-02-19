# Stormbot
A Discord.Net based bot.

### Highlights
* Dynamic Permissions (Role, user and channel Allows/Denies using JSON data)
* Audio streaming from basically most video/audio services (twitch included)
* Basic server management (editing users, roles, channels etc)
* Various announcements (user joined, user left)
* Auto add role on user joins.
* Executing C# code on demand
* Various quality of life features (custom quotes, reminders, custom colors, lmgtfy, coin flipping, vermintide roulette)
* Terraria bridge. (Traveling merchant ivnentory, event notifications, npc notifications, world info, server info etc it's basicalyl a custom terraria client)
* Twitch chat <-> Discord bridge
* Global Twitch and global BTTV emotes.

These are just the main features of this bot. For a comprehensive list of commands see [commands.md](https://github.com/SSStormy/Stormbot/blob/master/docs/commands.md)

Dynamic permissions documentation: [dynperm.md](https://github.com/SSStormy/Stormbot/blob/master/docs/dynperm.md)

#### Branches
Master - what's running on `BeepBoop` bot

Dev - unfinished, untested, or even broken changes.

### Contact
The easiest way you can contact me is via the StormBot test server over [here](https://discord.gg/0lHgknA1Q2RIJK0m)

## Compiling

###### Prerequisites.
* C#6
* .NET 4.6
* StrmyCore
* Discord.Net (slightly modified)
* Discord.Net Modules, Commands, Audio
* A Discord account the bot can log into.
* Livestreamer, ffmpeg, ffprobe, YoutubeExtractor and a Soundcloud API key for audio.
* Microsoft.CodeAnalysis.Scripting for the execute module.
* TerrariaBridge for the Terraria module. (https://github.com/SSStormy/TerrariaBridge)
* TwitchBotBase for the Twitch bridge (https://github.com/SSStormy/TwitchBotBase)
* Pastebin user credentials and a API key

###### Data

You will first want to set up a Data folder (stored in ````Stormbot.Helpers.Constants.DataFolderDir````). 

After that you will need to create a ````config.json```` and fill it with information in the given json format:
````
{
  "Email"            : "",
  "Password"         : "",

  // only if using audio
  "FfmpegDir"        : "",
  "FfprobeDir"       : "", 
  "SoundcloudApiKey" : "",
  "LivestreamerDir"  : "",
  
  // only if using the twitch bridge
  "TwitchOauth"      : "",
  "TwitchUsername"   : "",
  
  "PastebinApiKey"   : "",
  "PastebinUsername" : "",
  "PastebinPassword" : ""
}

````
Compile the bot, have it log in with the details we gave it in ````config.json````.
Then you will want to give it the owner id, stored in ````Stormbot.Helpers.Constants.UserOwner````. You can use the !whoami command to find out your user id.
