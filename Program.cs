using System;
using System.IO;
using Newtonsoft.Json;
using Stormbot.Bot.Core;
using Stormbot.Bot.Core.Modules.Audio;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (File.Exists(Constants.ConfigDir))
            {
                dynamic config = JsonConvert.DeserializeObject(File.ReadAllText(Constants.ConfigDir));
                try
                {
                    Constants.Pass = config.Password;
                    Constants.FfprobeDir = config.FfprobeDir;
                    Constants.FfmpegDir = config.FfmpegDir;
                    Constants.LivestreamerDir = config.LivestreamerDir;
                    SoundcloudResolver.ApiKey = config.SoundcloudApiKey;
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite("Entry", $"Failed parsing config.json. Exception: {ex}");
                    Console.ReadLine();
                    return;
                }
                StormBot bot = new StormBot((string)config.Email, (string)config.Password);
                bot.Start();
            }
            else
                Logger.Writeline("config.json was not found.");

            Logger.Writeline("Press any key to exit...");
            Console.ReadLine();
        }
    }
}