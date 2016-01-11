// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Diagnostics;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class LivestreamerResolver : IStreamResolver
    {
        public TrackResolveResult Resolve(string input)
        {
            TrackResolveResult retval = new TrackResolveResult();

            using (Process livestreamer = new Process
            {
                StartInfo =
                    {
                        FileName = Constants.LivestreamerDir,
                        Arguments = $"--stream-url {input} best",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
                EnableRaisingEvents = true
            })
            {
                livestreamer.OutputDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(args.Data)) return;

                    if (args.Data.StartsWith("error"))
                    {
                        retval.Message = args.Data;
                        return;
                    }

                    retval.Track = new TrackData(args.Data, input);
                    retval.WasSuccessful = true;
                };

                if (!livestreamer.Start())
                {
                    Logger.FormattedWrite(typeof(TrackData).Name, "Failed starting livestreamer.",
                        ConsoleColor.Red);
                    retval.Message = "Failed starting livestreamer.";
                }
                livestreamer.BeginOutputReadLine();
                livestreamer.WaitForExit();
                return retval;
            }
        }

        public bool CanResolve(string input)
        {
            return true;
        }
    }
}
