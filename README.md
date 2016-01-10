# Stormbot
A Discord.Net based personal bot.

# Prerequisites.
* C#6
* .NET 4.5.2
* StrmyCore
* Discord.Net, Modules, Commands, Audio
* A Discord account the bot can log into.
* Livestreamer, ffmpeg and ffprobe for audio.

You will first want to set up a Data folder (stored in ````Stormbot.Helpers.Constants.DataFolderDir````). 

After that you will need to create a ````config.json```` and fill it with information in the given json format:
````
{
  "Email"           : "",
  "Password"        : "",
  "FfmpegDir"       : "",
  "FfprobeDir"      : "",
  "LivestreamerDir" : ""
}

````
Compile the bot, have it log in with the details we gave it in ````config.json````.
Then you will want to give it the owner id, stored in ````Stormbot.Helpers.Constants.UserOwner````. You can use the !whoami command to find out your user id.

