using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Newtonsoft.Json;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public class AudioStreamModule : IDataModule
    {
        [JsonObject(MemberSerialization.OptIn)]
        private class AudioState
        {
            private Server _hostServer;
            private Channel _playbackVoiceChannel;

            private IAudioClient _voiceClient;
            private DiscordClient _client;

            [JsonProperty] private int _trackIndex;

            public int TrackIndex
            {
                get { return _trackIndex; }
                set
                {
                    if (value < 0)
                        _trackIndex = Playlist.Count - 1;
                    else if (value >= Playlist.Count)
                        _trackIndex = 0;

                    else _trackIndex = value;
                }
            }

            ///<summary>Returns the track that is supposed to be played at this moment.</summary>
            public TrackData CurrentTrack => Playlist[TrackIndex];

            ///<summary>Returns the server this audio state belongs to.</summary>
            public Server HostServer
            {
                get { return _hostServer; }
                set
                {
                    _hostServer = value;
                    HostServerId = value.Id;
                }
            }

            public Channel PlaybackChannel
            {
                get { return _playbackVoiceChannel; }
                set
                {
                    _playbackVoiceChannel = value;
                    PlaybackChannelId = value.Id;
                }
            }

            [JsonProperty]
            public List<TrackData> Playlist { get; }

            [JsonProperty]
            private ulong HostServerId { get; set; }

            [JsonProperty]
            private ulong PlaybackChannelId { get; set; }

            public Channel ChatChannel { get; set; }

            public bool IsPlaying { get; private set; }

            private bool _stopTrackFlag; // stops playback of the currently played track.
            private bool _stopPlaylistFlag; // stops playback of the track and playlist.
            private bool _pausePlaybackFlag; // pauses playback of the currently played track.
            private bool _prevFlag; // set when we want to go one track back.

            private bool _skipToFlag; // set when we want to skip to a ceratin point in the currently played track or in the playlist

            // the time to which we will skip in the currently played track when the skiptoflag is set.
            private TimeSpan _skipTime;

            [JsonConstructor]
            private AudioState(ulong hostServerId, List<TrackData> playlist = null, ushort trackIndex = 0, ulong playbackChannelId = 0)
            {
                HostServerId = hostServerId;
                Playlist = playlist ?? new List<TrackData>();
                TrackIndex = trackIndex;
                PlaybackChannelId = playbackChannelId;
            }

            public AudioState(Server host, DiscordClient client) : this(host.Id, null)
            {
                HostServer = host;
                _client = client;
            }

            public void FinishLoading(DiscordClient client)
            {
                if (HostServer != null) throw new InvalidOperationException();

                HostServer = client.GetServer(HostServerId);
                _client = client;
            }

            public void ClearPlaylist()
            {
                _stopPlaylistFlag = true;
                Playlist.Clear();
            }

            public void StopPlaylist()
            {
                _stopPlaylistFlag = true;
                _stopTrackFlag = true;
            }

            public async Task StartPlaylist()
            {
                if (IsPlaying) return;

                if (await IsPlaylistEmpty())
                    return;

                if (PlaybackChannel == null)
                {
                    await ChatChannel.SendMessage("Audio playback channel has not been set.");
                    return;
                }

                if (PlaybackChannel.Type != ChannelType.Voice)
                {
                    await ChatChannel.SendMessage("Audio playback channel type is not voice.");
                    return;
                }

                _voiceClient = await _client.Audio().Join(PlaybackChannel);

                while (true)
                {
                    if (_stopPlaylistFlag)
                    {
                        _stopPlaylistFlag = false;
                        break;
                    }

                    if (await IsPlaylistEmpty())
                        return;

                    await PrintCurrentTrack();
                    await StartCurrentTrackPlayback();

                    if (_prevFlag)
                    {
                        _prevFlag = false;
                        TrackIndex--;
                    }
                    else if (!_skipToFlag)
                        TrackIndex++;
                }

                _voiceClient.Wait();
                await _voiceClient.Disconnect();
            }

            private async Task<bool> IsPlaylistEmpty()
            {
                if (!Playlist.Any())
                {
                    await ChatChannel.SendMessage("No tracks in playlist.");
                    return true;
                }
                return false;
            }

            private async Task StartCurrentTrackPlayback()
            {
                if (IsPlaying) return;

                if (PlaybackChannel == null)
                {
                    await ChatChannel.SendMessage("Playback channel wasn't set when attempting to start track playback.");
                    return;
                }

                if (_voiceClient == null)
                {
                    await ChatChannel.SendMessage("Voice client wasn't set when attempting to start track playback.");
                    return;
                }

                _stopTrackFlag = false;
                IsPlaying = true;

                using (AudioStreamer streamer = new AudioStreamer(CurrentTrack.GetStream(), _client))
                {
                    if (_skipToFlag)
                    {
                        streamer.Start(_skipTime);
                        _skipTime = TimeSpan.Zero;
                        _skipToFlag = false;
                    }
                    else
                        streamer.Start();


                    int bufferSize = 1920*_client.Audio().Config.Channels;
                    byte[] buffer = new byte[bufferSize];

                    // Wait for the ffmpeg stream to become available.
                    while (streamer.OutputStream == null) await Task.Delay(10);

                    while (true)
                    {
                        if (_stopTrackFlag)
                        {
                            _pausePlaybackFlag = false;
                            _stopTrackFlag = false;
                            break;
                        }
                        if (_pausePlaybackFlag)
                            await Task.Delay(100);
                        else
                        {
                            if (streamer.OutputStream.ReadExactly(buffer, bufferSize))
                                break;
                            _voiceClient.Send(buffer, 0, bufferSize);
                        }
                    }
                }
                IsPlaying = false;
            }

            public void StopPlayback()
            {
                _stopTrackFlag = true;
                _pausePlaybackFlag = false;
            }

            public void ForceStop()
            {
                _voiceClient?.Disconnect();
                IsPlaying = false;
            }

            public void Pause(bool? val = null)
            {
                if (val == null)
                    val = !_pausePlaybackFlag;

                _pausePlaybackFlag = val.Value;
            }

            public void SkipToTimeInTrack(TimeSpan time)
            {
                if (!IsPlaying) return;
                if (time >= CurrentTrack.Length) return;

                _stopTrackFlag = true;
                _pausePlaybackFlag = false;
                _skipToFlag = true;

                _skipTime = time;
            }

            public void SkipToTrack(int index)
            {
                if (!IsPlaying) return;

                _stopTrackFlag = true;
                _pausePlaybackFlag = false;
                _skipToFlag = true;

                TrackIndex = index;
            }

            public void Previous()
            {
                _prevFlag = true;
                StopPlayback();
            }

            public async Task PrintCurrentTrack()
            {
                try
                {
                    if (CurrentTrack != null)
                        await
                            ChatChannel.SendMessage(
                                $"Currently playing: `{CurrentTrack.Name}` [`{CurrentTrack.Length}`]");
                    else
                        await ChatChannel.SendMessage("No track playing");
                }
                catch (IndexOutOfRangeException)
                {
                    await
                        ChatChannel.SendMessage(
                            $"Welp something went wrong with the Track index, which has been reset so try again. \r\n Debug: index {TrackIndex} size {Playlist.Count}");
                    TrackIndex = 0;
                }
            }
        }

        private DiscordClient _client;

        [DataSave, DataLoad]
        private ConcurrentDictionary<ulong, AudioState> _audioStates;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("stream", group =>
            {
                // commands which can only be called when there is a track currently playing.
                group.CreateGroup("", playingGroup =>
                {
                    playingGroup.AddCheck((cmd, usr, chnl) => GetAudio(chnl).IsPlaying);

                    playingGroup.CreateCommand("goto")
                        .Description("Skips to the given point in the track.")
                        .Parameter("time")
                        .Do(e => GetAudio(e.Channel).SkipToTimeInTrack(TimeSpan.Parse(e.GetArg("time"))));

                    playingGroup.CreateCommand("stop")
                        .Description("Stops playback of the playlist.")
                        .Do(e => GetAudio(e.Channel).StopPlaylist());

                    playingGroup.CreateCommand("forcestop")
                        .Description("Forcefully stops playback of the playlist, track and leaves the voice channel.")
                        .MinPermissions((int) PermissionLevel.ChannelAdmin)
                        .Do(e => GetAudio(e.Channel).ForceStop());

                    playingGroup.CreateCommand("next")
                        .Description("Skips the current track and plays the next track in the playlist.")
                        .Do(e => GetAudio(e.Channel).StopPlayback());

                    playingGroup.CreateCommand("prev")
                        .Description("Skips the current track and plays the previus track in the playlist.")
                        .Do(e => GetAudio(e.Channel).Previous());

                    playingGroup.CreateCommand("current")
                        .Description("Displays information about the currently played track.")
                        .Do(async e => await GetAudio(e.Channel).PrintCurrentTrack());

                    playingGroup.CreateCommand("toggle")
                        .Alias("pause")
                        .Description("Pauses/unpauses playback of the current track.")
                        .Do(e => GetAudio(e.Channel).Pause());
                });

                // commands which can only be called when there is no track playing.
                group.CreateGroup("", idleGroup =>
                {
                    idleGroup.AddCheck((cmd, usr, chnl) => !GetAudio(chnl).IsPlaying);

                    idleGroup.CreateCommand("start")
                        .Alias("play")
                        .Description("Starts the playback of the playlist.")
                        .Do(async e =>
                        {
                            AudioState audio = GetAudio(e.Channel);

                            if (audio.PlaybackChannel == null && e.User.VoiceChannel != null) audio.PlaybackChannel = e.User.VoiceChannel;

                            if (audio.PlaybackChannel == null)
                            {
                                await e.Channel.SendMessage("Playback channel not set.");
                                return;
                            }

                            await audio.StartPlaylist();
                        });
                    idleGroup.CreateCommand("channel")
                        .Description(
                            "Sets the channel in which the audio will be played in. Use .c to set it to your current channel.")
                        .Parameter("channel", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            AudioState audio = GetAudio(e.Channel);

                            string chnl = e.GetArg("channel");
                            audio.PlaybackChannel = chnl == ".c"
                                ? e.User.VoiceChannel
                                : e.Server.FindChannels(e.GetArg("channel"), ChannelType.Voice).FirstOrDefault();

                            if (audio.PlaybackChannel != null)
                                await
                                    e.Channel.SendMessage($"Set playback channel to \"`{audio.PlaybackChannel.Name}`\"");
                        });
                    // todo : move to channel command
                });


                group.CreateCommand("add")
                    .Description("Adds a track to the music playlist.")
                    .Parameter("location", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string loc = e.GetArg("location");
                        TrackData result = TrackData.Parse(loc);

                        if (result == null)
                        {
                            await e.Channel.SendMessage($"Failed getting the stream url for `{loc}.");
                            return;
                        }

                        GetAudio(e.Channel).Playlist.Add(result);
                        await e.Channel.SendMessage($"Added `{result.Name}` to the playlist.");
                    });

                group.CreateCommand("setpos")
                    .Alias("set")
                    .Description("Sets the position of the current played track index to a given number.")
                    .Parameter("index")
                    .Do(e => GetAudio(e.Channel).SkipToTrack(int.Parse(e.GetArg("index")) - 1));

                group.CreateCommand("remove")
                    .Alias("rem")
                    .Description("Removes a track at the given position from the playlist.")
                    .Parameter("index")
                    .Do(async e =>
                    {
                        AudioState audio = GetAudio(e.Channel);

                        int remIndex = int.Parse(e.GetArg("index")) - 1;

                        TrackData remData = audio.Playlist[remIndex];
                        audio.Playlist.RemoveAt(remIndex);

                        await e.Channel.SendMessage($"Removed track `{remData.Name}` from the playlist.");
                    });
                group.CreateCommand("list")
                    .Description("List the songs in the current playlist.")
                    .Do(async e =>
                    {
                        AudioState audio = GetAudio(e.Channel);

                        if (!audio.Playlist.Any())
                        {
                            await e.Channel.SendMessage("Playlist is empty.");
                            return;
                        }
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("**Playlist:**");

                        for (int i = 0; i < audio.Playlist.Count; i++)
                        {
                            if (i == audio.TrackIndex && audio.IsPlaying)
                                builder.Append("Playing: ");
                            builder.AppendLine($"`{i + 1}: {audio.Playlist[i].Name}`");
                        }

                        await e.Channel.SendMessage(builder.ToString());
                    });
                group.CreateCommand("clear")
                    .Description("Stops music and clears the playlist.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(e => GetAudio(e.Channel).ClearPlaylist());
            });
        }

        private AudioState GetAudio(Channel chat)
        {
            if (!_audioStates.ContainsKey(chat.Server.Id))
                _audioStates.TryAdd(chat.Server.Id, new AudioState(chat.Server, _client));

            AudioState state = _audioStates[chat.Server.Id];
            state.ChatChannel = chat;

            return _audioStates[chat.Server.Id];
        }

        public void OnDataLoad()
        {
            if(_audioStates == null)
                _audioStates = new ConcurrentDictionary<ulong, AudioState>();
            foreach (AudioState state in _audioStates.Values)
                state.FinishLoading(_client);
        }
    }
}
