using System;
using System.Diagnostics;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public sealed class LivestreamerResolver : IStreamResolver
    {
        public string ResolveStreamUrl(string input)
        {
            string retval = string.Empty;

            using (Process livestreamer = StartLivestreamer(input, (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;

                if (args.Data.StartsWith("error"))
                {
                    Logger.FormattedWrite(GetType().Name,
                        $"Livestreamer returned error while parsing input {input}. Error: {args.Data}");
                    return;
                }

                retval = args.Data;
            }))
                livestreamer.WaitForExit();

            return retval;
        }

        public bool CanResolve(string input)
        {
            bool retval = false;

            // let livestreamer decide whether it can handle the input.
            using (Process livestreamer = StartLivestreamer(input, (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;

                if (args.Data.StartsWith("error")) retval = false;
                retval = true;
            }))
                livestreamer.WaitForExit();

            return retval;
        }

        private Process StartLivestreamer(string inputUrl, DataReceivedEventHandler onData)
        {
            Process livestreamer = new Process
            {
                StartInfo =
                {
                    FileName = Constants.LivestreamerDir,
                    Arguments = $"--stream-url {inputUrl} best",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            livestreamer.OutputDataReceived += onData;

            if (!livestreamer.Start())
                Logger.FormattedWrite(typeof(TrackData).Name, "Failed starting livestreamer.",
                    ConsoleColor.Red);

            livestreamer.BeginOutputReadLine();
            return livestreamer;
        }

        public string GetTrackName(string input) => input;
    }
}