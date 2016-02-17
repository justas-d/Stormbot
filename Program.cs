using System;
using System.IO;
using Newtonsoft.Json;
using Stormbot.Bot.Core;
using Stormbot.Bot.Core.Modules.Audio;
using StrmyCore;

namespace Stormbot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (File.Exists(Constants.CredentialsConfigDir) &&
                File.Exists(Constants.CommonConfigDir))
            {
                dynamic credentials = JsonConvert.DeserializeObject(File.ReadAllText(Constants.CredentialsConfigDir));
                dynamic config = JsonConvert.DeserializeObject(File.ReadAllText(Constants.CommonConfigDir));
                try
                {
                    Config.Pass = credentials.Password;
                    Config.FfprobeDir = config.FfprobeDir;
                    Config.FfmpegDir = config.FfmpegDir;
                    Config.LivestreamerDir = config.LivestreamerDir;
                    Config.SoundcloudApiKey = config.SoundcloudApiKey;
                    Config.TwitchOauth = config.TwitchOauth;
                    Config.TwitchUsername = config.TwitchUsername;
                    Config.PastebinApiKey = config.PastebinApiKey;
                    Config.PastebinUsername = config.PastebinUsername;
                    Config.PastebinPassword= config.PastebinPassword;
                    Config.CommandsMdDir = config.CommandsMdDir;
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite("Entry", $"Failed parsing config.json. Exception: {ex}");
                    Console.ReadLine();
                    return;
                }
                StormBot bot = new StormBot((string)credentials.Email, (string)credentials.Password);
                bot.Start();
            }
            else
                Logger.Writeline("credentials.json was not found.");

            Logger.Writeline("Press any key to exit...");
            Console.ReadLine();
        }
    }
}