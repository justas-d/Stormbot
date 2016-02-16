using System;
using System.Diagnostics;
using System.IO;
using Discord;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal class AudioStreamer : IDisposable
    {
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

        public void Start(TimeSpan startTime) => StartFfmpeg($"-ss {startTime} {DefaultStartArgs}");
        public void Start() => StartFfmpeg(DefaultStartArgs);

        private void StartFfmpeg(string args)
        {
            _ffmpeg = new Process
            {
                StartInfo =
                {
                    FileName = Constants.FfmpegDir,
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
            Constants.StreamingJobs++;
            OutputStream = _ffmpeg.StandardOutput.BaseStream;
        }

        public void Dispose()
        {
            Constants.StreamingJobs--;
            OutputStream?.Dispose();
            _ffmpeg?.Dispose();
        }
    }
}