// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Diagnostics;
using System.IO;
using Discord;
using Discord.Audio;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal class AudioStreamer : IDisposable
    {
        private string Location { get; }
        private DiscordClient Client { get; }

        public Stream OutputStream { get; private set; }
        private Process _ffmpeg;

        public AudioStreamer(string location, DiscordClient client)
        {
            Location = location;
            Client = client;
        }

        public void Start()
        {
            _ffmpeg = new Process
            {
                StartInfo =
                    {
                        FileName = Constants.FfmpegDir,
                        Arguments = $"-i \"{Location}\" -f s16le -ar 48000 -ac {Client.Audio().Config.Channels} pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
            };
            if (!_ffmpeg.Start())
            {
                Logger.FormattedWrite(GetType().Name, "Failed starting ffmpeg.", ConsoleColor.Red);
                return;
            }
            OutputStream = _ffmpeg.StandardOutput.BaseStream;
        }

        public void Dispose()
        {
            OutputStream?.Dispose();
            _ffmpeg?.Dispose();
        }
    }
}
