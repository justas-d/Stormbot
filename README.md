# Stormbot
A Discord.Net based personal bot.

# Prerequisites.
* C#6
* .NET 4.6
* StrmyCore
* Discord.Net, Modules, Commands, Audio
* A Discord account the bot can log into.
* Livestreamer, ffmpeg, ffprobe, YoutubeExtractor and a Soundcloud API key for audio.
* Microsoft.CodeAnalysis.Scripting for the execute module.
* TerrariaBridge for the Terraria module. (https://github.com/SSStormy/TerrariaBridge)

# Highlights
* Basic server management (editing users, roles, channels etc)
* Executing C# code on demand
* Roles with custom hex colors
* Reminders
* Audio streaming from basically most of video/audio services (twitch included)
* A chat bridge between Terraria and Discord

# Setup
You will first want to set up a Data folder (stored in ````Stormbot.Helpers.Constants.DataFolderDir````). 

After that you will need to create a ````config.json```` and fill it with information in the given json format:
````
{
  "Email"            : "",
  "Password"         : "",

  //only if using audio
  "FfmpegDir"        : "",
  "FfprobeDir"       : "", 
  "SoundcloudApiKey" : "",
  "LivestreamerDir"  : ""
}

````
Compile the bot, have it log in with the details we gave it in ````config.json````.
Then you will want to give it the owner id, stored in ````Stormbot.Helpers.Constants.UserOwner````. You can use the !whoami command to find out your user id.

# Contact
If you wish to contact me you may do so whether it may be email or what ever you prefer. You can usualy find me hanging out in the Discord API channel over here: https://discordapp.com/invite/0SBTUU1wZTV9JAsL

