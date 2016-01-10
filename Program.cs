// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.IO;
using Newtonsoft.Json;
using Stormbot.Bot.Core;
using Stormbot.Helpers;
using Logger = Stormbot.Bot.Core.Logger;

namespace Stormbot
{
    internal static class Program
    {
        private static void Main()
        {
            if (File.Exists(Constants.ConfigFileDir))
            {
                dynamic config = JsonConvert.DeserializeObject(File.ReadAllText(Constants.ConfigFileDir));
                try
                {
                    Constants.Pass = config.Password;
                    Constants.FfprobeDir = config.FfprobeDir;
                    Constants.FfmpegDir = config.FfmpegDir;
                    Constants.LivestreamerDir = config.LivestreamerDir;
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