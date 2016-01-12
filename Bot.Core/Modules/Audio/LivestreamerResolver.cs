// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Diagnostics;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class LivestreamerResolver : IStreamResolver
    {
        public TrackData Resolve(string input)
        {
            TrackData retval = null;

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
                        Logger.FormattedWrite(GetType().Name, $"Livestreamer returned error: {args.Data}");
                        return;
                    }

                    retval = new TrackData(args.Data, input);
                };

                if (!livestreamer.Start())
                    Logger.FormattedWrite(typeof(TrackData).Name, "Failed starting livestreamer.",
                        ConsoleColor.Red);

                livestreamer.BeginOutputReadLine();
                livestreamer.WaitForExit();
                return retval;
            }
        }

        public bool CanResolve(string input) => true;
    }
}
