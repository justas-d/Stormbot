// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    [DataModule]
    public class AudioStreamModule : IModule
    {
        private class AudioStreamer : IDisposable
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

        [Serializable]
        internal class MusicData
        {
            public string Location { get; }
            public TimeSpan Length { get; private set; }
            public string Name { get; set; }

            [JsonConstructor, UsedImplicitly]
            private MusicData(string location, TimeSpan length, string name)
            {
                Location = location;
                Length = length;
                Name = name;
            }

            internal MusicData(string location)
            {
                Location = location;
                ReadTrackLengthFromDisk();
            }

            private MusicData(string location, string name) : this(location)
            {
                Name = name;
            }

            internal static MusicData Create(string location)
            {
                if (File.Exists(location))
                    return new MusicData(location) { Name = location.GetFilename() };

                using (Process livestreamer = new Process
                {
                    StartInfo =
                    {
                        FileName = Constants.LivestreamerDir,
                        Arguments = $"--stream-url {location} best",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
                    EnableRaisingEvents = true
                })
                {
                    MusicData retval = null;
                    livestreamer.OutputDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrEmpty(args.Data)) return;
                        if (args.Data.StartsWith("error")) return;

                        retval = new MusicData(args.Data, location);
                    };

                    if (!livestreamer.Start())
                    {
                        Logger.FormattedWrite(typeof(MusicData).Name, "Failed starting livestreamer.",
                            ConsoleColor.Red);
                        return null;
                    }
                    livestreamer.BeginOutputReadLine();
                    livestreamer.WaitForExit();
                    return retval;
                }
            }

            private void ReadTrackLengthFromDisk()
            {
                try
                {
                    using (Process ffprobe = new Process
                    {
                        StartInfo =
                        {
                            FileName = Constants.FfprobeDir,
                            Arguments =
                                $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{Location}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                        },
                        EnableRaisingEvents = true
                    })
                    {
                        ffprobe.OutputDataReceived += (sender, args) =>
                        {
                            if (string.IsNullOrEmpty(args.Data)) return;
                            if (args.Data == "N/A") Length = TimeSpan.Zero;
                            else
                            {
                                Length = TimeSpan.FromSeconds(
                                    int.Parse(
                                        args.Data.Remove(
                                            args.Data.IndexOf('.'))));
                            }
                        };

                        if (!ffprobe.Start())
                        {
                            Logger.FormattedWrite(GetType().Name, "Failed starting ffprobe.", ConsoleColor.Red);
                            return;
                        }

                        ffprobe.BeginOutputReadLine();
                    }
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite(GetType().Name, $"Failed getting track length. Exception: {ex}",
                        ConsoleColor.Red);
                }
            }
        }

        private DiscordClient _client;

        private Channel _audioPlaybackChannel;
        private int _trackIndex;

        private int TrackIndex
        {
            get { return _trackIndex; }
            set
            {
                if (value < 0)
                    _trackIndex = _playlist.Count - 1;
                else if (value >= _playlist.Count)
                    _trackIndex = 0;
                else _trackIndex = value;
            }
        }

        private bool _isPlaying;

        private bool _stopTrack;
        private bool _stopPlaylist;
        private bool _pauseTrack;
        private bool _prevFlag;

        [DataSave] [DataLoad] private readonly List<MusicData> _playlist = new List<MusicData>();
        private MusicData CurrentTrack => _playlist[TrackIndex];

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("stream", group =>
            {
                group.MinPermissions((int) PermissionLevel.Trusted);

                group.CreateCommand("add")
                    .Description("Adds a track to the music playlist.")
                    .Parameter("location", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string loc = e.GetArg("location");
                        MusicData track = MusicData.Create(loc);
                        if (track == null)
                        {
                            await e.Channel.SendMessage($"Failled adding `{loc}` to the playlist.");
                            return;
                        }
                        _playlist.Add(track);
                        await e.Channel.SendMessage($"Added `{track.Name}` to the playlist.");
                    });
                group.CreateCommand("raw")
                    .Description("Adds a raw stream to the track playlist.")
                    .Parameter("url", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string url = e.GetArg("url");
                        MusicData track = new MusicData(url) {Name = $"Raw stream: {url}"};

                        _playlist.Add(track);
                        await e.Channel.SendMessage($"Added `{track.Name}` to the playlist.");
                    });

                group.CreateCommand("setpos")
                    .Alias("set")
                    .Description("Sets the position of the current played track index to a given number.")
                    .Parameter("index")
                    .Do(async e =>
                    {
                        int newPos = int.Parse(e.GetArg("index")) - 1;
                        TrackIndex = newPos;
                        await
                            e.Channel.SendMessage(
                                $"Set track index to `{TrackIndex + 1}`: `{_playlist[TrackIndex].Name}`");
                        _stopTrack = true;
                    });

                group.CreateCommand("stop")
                    .Description("Stops playback of the playlist.")
                    .Do(e =>
                    {
                        _stopTrack = true;
                        _stopPlaylist = true;
                    });
                group.CreateCommand("next")
                    .Description("Skips the current track and plays the next track in the playlist.")
                    .Do(e =>
                    {
                        _stopTrack = true;
                    });
                group.CreateCommand("prev")
                    .Description("Skips the current track and plays the previus track in the playlist.")
                    .Do(e =>
                    {
                        _prevFlag = true;
                        _stopTrack = true;
                        _trackIndex--;
                    });
                group.CreateCommand("start")
                    .Alias("play")
                    .Description("Starts the playback of the playlist.")
                    .Do(async e =>
                    {
                        if (_audioPlaybackChannel == null) _audioPlaybackChannel = e.User.VoiceChannel;
                        await StartPlaylistPlayback(e.Channel);
                    });
                group.CreateCommand("channel")
                    .Description(
                        "Sets the channel in which the audio will be played in. Use .c to set it to your current channel.")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string chnl = e.GetArg("channel");
                        _audioPlaybackChannel = chnl == ".c"
                            ? e.User.VoiceChannel
                            : e.Server.FindChannels(e.GetArg("channel"), ChannelType.Voice).FirstOrDefault();

                        if (_audioPlaybackChannel != null)
                            await e.Channel.SendMessage($"Set playback channel to \"`{_audioPlaybackChannel.Name}`\"");
                    });
                group.CreateCommand("remove")
                    .Alias("rem")
                    .Description("Removes a track at the given position from the playlist.")
                    .Parameter("index")
                    .Do(async e =>
                    {
                        int remIndex = int.Parse(e.GetArg("index")) - 1;
                        MusicData remData = _playlist[remIndex];
                        _playlist.RemoveAt(remIndex);
                        await e.Channel.SendMessage($"Removed track `{remData.Name}` from the playlist.");
                    });
                group.CreateCommand("list")
                    .Description("List the songs in the current playlist.")
                    .Do(async e =>
                    {
                        if (!_playlist.Any())
                        {
                            await e.Channel.SendMessage("Playlist is empty.");
                            return;
                        }
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Playlist:**");

                        for (int i = 0; i < _playlist.Count; i++)
                        {
                            if (i == TrackIndex && _isPlaying)
                                builder.Append("Playing: ");
                            builder.AppendLine($"`{i + 1}: {_playlist[i].Name}`");
                        }

                        await e.Channel.SendMessage(builder.ToString());
                    });
                group.CreateCommand("clear")
                    .Description("Stops music and clears the playlist.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(e =>
                    {
                        _stopTrack = true;
                        _stopPlaylist = true;
                        _playlist.Clear();
                    });

                group.CreateCommand("toggle")
                    .Description("Pauses/unpauses playback of the current track.")
                    .Do(e =>
                    {
                        _pauseTrack = !_pauseTrack;
                    });
                group.CreateCommand("current")
                    .Description("Displays information about the currently played track.")
                    .Do(async e =>
                    {
                        await PrintCurrentTrack(e.Channel);
                    });
            });
        }

        private async Task PrintCurrentTrack(Channel channel)
        {
            try
            {
                if (CurrentTrack != null)
                    await
                        channel.SendMessage(
                            $"Currently playing: `{CurrentTrack.Name}` [`{CurrentTrack.Length.ToString("hh\\:mm\\:ss")}`]");
                else
                    await channel.SendMessage("No track playing");
            }
            catch (IndexOutOfRangeException)
            {
                await
                    channel.SendMessage(
                        $"Welp something went wrong with the Track index, which has been reset so try again. \r\n Debug: {TrackIndex} size: {_playlist.Count}");
                TrackIndex = 0;
            }
        }

        private async Task StartPlaylistPlayback(Channel textChannel)
        {
            if (_audioPlaybackChannel == null)
            {
                await textChannel.SendMessage("Audio playback channel has not been set.");
                return;
            }

            if (_audioPlaybackChannel.Type != ChannelType.Voice) return;
            if (_isPlaying) return;

            IAudioClient voice = await _client.Audio().Join(_audioPlaybackChannel);

            // try playing the playlist while track index is withing range of _playlist.s
            while (_playlist.Count > TrackIndex)
            {
                if (_stopPlaylist)
                {
                    _stopPlaylist = false;
                    break;
                }

                await StartAudioStream(textChannel, voice, CurrentTrack);

                if (_prevFlag)
                {
                    _prevFlag = false;
                    TrackIndex--;
                }
                else
                    TrackIndex++;
            }
            voice.Wait();
            await voice.Disconnect();
        }

        private async Task StartAudioStream(Channel text, IAudioClient voice, MusicData track)
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _client.SetGame(track.Name);

            using (var streamer = new AudioStreamer(track.Location, _client))
            {
                streamer.Start();

                int bufferSize = 1920*_client.Audio().Config.Channels;
                byte[] buffer = new byte[bufferSize];

                // Wait for the ffmpeg stream to become available.
                while (streamer.OutputStream == null) await Task.Delay(100);
                await PrintCurrentTrack(text);

                while (true)
                {
                    if (_stopTrack)
                    {
                        _pauseTrack = false;
                        _stopTrack = false;
                        break;
                    }
                    if (_pauseTrack)
                        await Task.Delay(100);
                    else
                    {
                        if (streamer.OutputStream.ReadExactly(buffer, bufferSize))
                            break;
                        voice.Send(buffer, 0, bufferSize);
                    }
                }
            }
            _client.SetGame("");
            _isPlaying = false;
        }
    }
}