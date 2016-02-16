using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public sealed class LivestreamerResolver : IStreamResolver
    {
        private Task StartLivestreamer(string inputUrl, DataReceivedEventHandler onData)
            => Task.Run(() =>
            {
                Process livestreamer = new Process
                {
                    StartInfo =
                    {
                        FileName = Constants.LivestreamerDir,
                        Arguments = $"--stream-url {inputUrl} best",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                livestreamer.OutputDataReceived += onData;

                if (!livestreamer.Start())
                    Logger.FormattedWrite(typeof (TrackData).Name, "Failed starting livestreamer.",
                        ConsoleColor.Red);

                livestreamer.BeginOutputReadLine();
                livestreamer.WaitForExit();

                livestreamer.OutputDataReceived -= onData;
            });

        bool IStreamResolver.SupportsTrackNames => false;
        bool IStreamResolver.SupportsAsyncCanResolve => true;

        async Task<string> IStreamResolver.ResolveStreamUrl(string input)
        {
            string retval = string.Empty;

            await StartLivestreamer(input, (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;

                if (args.Data.StartsWith("error"))
                {
                    Logger.FormattedWrite(GetType().Name,
                        $"Livestreamer returned error while parsing input {input}. Error: {args.Data}");
                    return;
                }

                retval = args.Data;
            });

            return retval;
        }

        async Task<bool> IStreamResolver.AsyncCanResolve(string input)
        {
            bool retval = false;

            // let livestreamer decide whether it can handle the input.
            await StartLivestreamer(input, (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;

                if (args.Data.StartsWith("error")) retval = false;
                retval = true;
            });

            return retval;
        }

        bool IStreamResolver.SyncCanResolve(string input)
        {
            throw new NotSupportedException();
        }

        Task<string> IStreamResolver.GetTrackName(string input)
        {
            throw new NotSupportedException();
        }
    }
}