# Stormbot
A Discord.Net based personal bot.

# Prerequisites.
* C#6
* .NET 4.5.2
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

Once all that is done you should be able to compile the code and run the bot.
