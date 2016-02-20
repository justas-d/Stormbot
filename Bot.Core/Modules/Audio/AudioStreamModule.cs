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
using Stormbot.Bot.Core.DynPerm;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public class AudioStreamModule : IDataObject, IModule
    {
        [JsonObject(MemberSerialization.OptIn)]
        private class AudioState
        {
            private DiscordClient _client;
            private Server _hostServer;

            /// <summary>pauses playback of the currently played track.</summary>
            private bool _pausePlaybackFlag;

            private Channel _playbackVoiceChannel;

            /// <summary>set when we want to go one track back.</summary>
            private bool _prevFlag;

            /// <summary>the time to which we will skip in the currently played track when the skiptoflag is set.</summary>
            private TimeSpan _skipTime;

            /// <summary>set when we want to skip to a ceratin point in the currently played track or in the playlist</summary>
            private bool _skipToFlag;

            /// <summary>stops playback of the track and playlist.</summary>
            private bool _stopPlaylistFlag;

            /// <summary>stops playback of the currently played track.</summary>
            private bool _stopTrackFlag;

            [JsonProperty] private int _trackIndex;
            private IAudioClient _voiceClient;

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

            /// <summary>Returns the track that is supposed to be played at this moment.</summary>
            public TrackData CurrentTrack => Playlist[TrackIndex];

            /// <summary>Returns the server this audio state belongs to.</summary>
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
                    if (value.Type != ChannelType.Voice)
                        return;

                    _playbackVoiceChannel = value;
                    PlaybackChannelId = value.Id;
                }
            }

            [JsonProperty]
            public List<TrackData> Playlist { get; }

            [JsonProperty]
            public ulong HostServerId { get; private set; }

            [JsonProperty]
            private ulong PlaybackChannelId { get; set; }

            public Channel ChatChannel { get; set; }
            public bool IsPlaying { get; private set; }

            [JsonConstructor]
            private AudioState(ulong hostServerId, List<TrackData> playlist = null, ushort trackIndex = 0,
                ulong playbackChannelId = 0)
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

            public bool FinishLoading(DiscordClient client)
            {
                if (HostServer != null) throw new InvalidOperationException();

                Server host = client.GetServer(HostServerId);

                if (host == null)
                    return false;

                HostServer = host;
                _client = client;
                return true;
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
                    await ChatChannel.SafeSendMessage("Audio playback channel has not been set.");
                    return;
                }

                if (!await DiscordUtils.CanJoinAndTalkInVoiceChannel(PlaybackChannel, ChatChannel))
                    return;

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

            public async Task SwitchChannel(Channel channel)
            {
                if (!IsPlaying) return;

                if (!await DiscordUtils.CanJoinAndTalkInVoiceChannel(channel, ChatChannel))
                    return;

                await _voiceClient.Join(channel);
                PlaybackChannel = channel;
            }

            private async Task<bool> IsPlaylistEmpty()
            {
                if (!Playlist.Any())
                {
                    await ChatChannel.SafeSendMessage("No tracks in playlist.");
                    return true;
                }
                return false;
            }

            private async Task StartCurrentTrackPlayback()
            {
                if (IsPlaying) return;

                if (PlaybackChannel == null)
                {
                    await
                        ChatChannel.SafeSendMessage(
                            "Playback channel wasn't set when attempting to start track playback.");
                    return;
                }

                if (_voiceClient == null)
                {
                    await
                        ChatChannel.SafeSendMessage("Voice client wasn't set when attempting to start track playback.");
                    return;
                }

                _stopTrackFlag = false;
                IsPlaying = true;

                using (AudioStreamer streamer = new AudioStreamer(await CurrentTrack.GetStream(), _client))
                {
                    if (_skipToFlag)
                    {
                        streamer.Start(_skipTime);
                        _skipTime = TimeSpan.Zero;
                        _skipToFlag = false;
                    }
                    else
                    {
                        await PrintCurrentTrack();
                        streamer.Start();
                    }

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
                    {
                        StringBuilder builder = new StringBuilder($"Currently playing: {CurrentTrack.Name}");
                        if (CurrentTrack.Length != null && CurrentTrack.Length != TimeSpan.Zero)
                            builder.Append($" `[{CurrentTrack.Length}]`");

                        await ChatChannel.SafeSendMessage(builder.ToString());
                    }

                    else
                        await ChatChannel.SafeSendMessage("No track playing");
                }
                catch (IndexOutOfRangeException)
                {
                    await
                        ChatChannel.SafeSendMessage(
                            $"Welp something went wrong with the Track index, which has been reset so try again. \r\n Debug: index {TrackIndex} size {Playlist.Count}");
                    TrackIndex = 0;
                }
            }
        }

        [DataSave, DataLoad] private ConcurrentDictionary<ulong, AudioState> _audioStates;
        private DiscordClient _client;

        void IDataObject.OnDataLoad()
        {
            if (_audioStates == null)
                _audioStates = new ConcurrentDictionary<ulong, AudioState>();
            foreach (AudioState state in _audioStates.Values)
            {
                if (!state.FinishLoading(_client))
                {
                    Logger.FormattedWrite("AudioLoad",
                        $"Tried to load AudioState for nonexistant server id : {state.HostServerId}. Removing",
                        ConsoleColor.Yellow);
                    _audioStates.Remove(state.HostServerId);
                }
            }
        }

        void IModule.Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateDynCommands("stream", PermissionLevel.User, group =>
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

                    playingGroup.CreateCommand("pause")
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
                        .Parameter("channel", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            AudioState audio = GetAudio(e.Channel);
                            string channelQuery = e.GetArg("channel");

                            if (string.IsNullOrEmpty(channelQuery))
                            {
                                if (e.User.VoiceChannel != null)
                                    audio.PlaybackChannel = e.User.VoiceChannel;
                            }
                            else
                            {
                                channelQuery = channelQuery.ToLowerInvariant();
                                Channel voiceChannel =
                                    e.Server.VoiceChannels.FirstOrDefault(
                                        c => c.Name.ToLowerInvariant().StartsWith(channelQuery));

                                if (voiceChannel == null)
                                {
                                    await
                                        e.Channel.SafeSendMessage(
                                            $"Voice channel with the name of {channelQuery} was not found.");
                                    return;
                                }
                                audio.PlaybackChannel = voiceChannel;
                            }

                            if (audio.PlaybackChannel == null)
                            {
                                await e.Channel.SafeSendMessage("Playback channel not set.");
                                return;
                            }

                            await audio.StartPlaylist();
                        });
                });

                group.CreateCommand("add")
                    .Description("Adds a track to the music playlist.")
                    .Parameter("location", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string loc = e.GetArg("location");
                        TrackData result = await TrackData.Parse(loc);

                        if (result == null)
                        {
                            await e.Channel.SafeSendMessage($"Failed getting the stream url for `{loc}.");
                            return;
                        }

                        GetAudio(e.Channel).Playlist.Add(result);
                        await e.Channel.SafeSendMessage($"Added `{result.Name}` to the playlist.");
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

                        await e.Channel.SafeSendMessage($"Removed track `{remData.Name}` from the playlist.");
                    });
                group.CreateCommand("list")
                    .Description("List the songs in the current playlist.")
                    .Do(async e =>
                    {
                        AudioState audio = GetAudio(e.Channel);

                        if (!audio.Playlist.Any())
                        {
                            await e.Channel.SafeSendMessage("Playlist is empty.");
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

                        await e.Channel.SafeSendMessage(builder.ToString());
                    });
                group.CreateCommand("clear")
                    .Description("Stops music and clears the playlist.")
                    .MinPermissions((int) PermissionLevel.ServerModerator)
                    .Do(e => GetAudio(e.Channel).ClearPlaylist());

                group.CreateCommand("channel")
                    .Description(
                        "Sets the channel in which the audio will be played in. Use .c to set it to your current channel.")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        AudioState audio = GetAudio(e.Channel);
                        string channelQuery = e.GetArg("channel");
                        Channel channel = channelQuery == ".c"
                            ? e.User.VoiceChannel
                            : e.Server.FindChannels(channelQuery, ChannelType.Voice).FirstOrDefault();

                        if (channel == null)
                        {
                            await e.Channel.SafeSendMessage($"Voice channel `{channelQuery}` not found.");
                            return;
                        }

                        if (audio.IsPlaying)
                            await audio.SwitchChannel(channel);
                        else
                        {
                            audio.PlaybackChannel = channel;
                            await
                                e.Channel.SafeSendMessage($"Set playback channel to \"`{audio.PlaybackChannel.Name}`\"");
                        }
                    });
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
    }
}