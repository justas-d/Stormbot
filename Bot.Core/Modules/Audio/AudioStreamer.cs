using System;
using System.Diagnostics;
using System.IO;
using Discord;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal class AudioStreamer : IDisposable
    {
        internal static int StreamingJobs = 0;

        private Process _ffmpeg;
        private string Location { get; }
        private DiscordClient Client { get; }
        public Stream OutputStream { get; private set; }

        private string DefaultStartArgs
            => $"-i \"{Location}\" -f s16le -ar 48000 -ac {Client.Audio().Config.Channels} pipe:1";

        public AudioStreamer(string location, DiscordClient client)
        {
            Location = location;
            Client = client;
        }

        void IDisposable.Dispose()
        {
            StreamingJobs--;
            OutputStream?.Dispose();
            _ffmpeg?.Dispose();
        }

        public void Start(TimeSpan startTime) => StartFfmpeg($"-ss {startTime} {DefaultStartArgs}");
        public void Start() => StartFfmpeg(DefaultStartArgs);

        private void StartFfmpeg(string args)
        {
            _ffmpeg = new Process
            {
                StartInfo =
                {
                    FileName = Config.FfmpegDir,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            if (!_ffmpeg.Start())
            {
                Logger.FormattedWrite(GetType().Name, "Failed starting ffmpeg.", ConsoleColor.Red);
                return;
            }
            StreamingJobs++;
            OutputStream = _ffmpeg.StandardOutput.BaseStream;
        }
    }
}