// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    [DataModule]
    public class AudioStreamModule : IModule
    {
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

        [DataSave] [DataLoad] private readonly List<TrackData> _playlist = new List<TrackData>();
        private TrackData CurrentTrack => _playlist[TrackIndex];

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
                        TrackResolveResult result = TrackData.Create(loc);

                        if (result == null)
                        {
                            await e.Channel.SendMessage($"Failed getting the stream url for `{loc}. Info: {result.Message}`");
                            return;
                        }

                        _playlist.Add(result.Track);
                        await e.Channel.SendMessage($"Added `{result.Track.Name}` to the playlist.");
                    });
                group.CreateCommand("raw")
                    .Description("Adds a raw stream to the track playlist.")
                    .Parameter("url", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string url = e.GetArg("url");
                        TrackData track = new TrackData(url, $"Raw stream: {url}");

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
                        TrackData remData = _playlist[remIndex];
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

        private async Task StartAudioStream(Channel text, IAudioClient voice, TrackData track)
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