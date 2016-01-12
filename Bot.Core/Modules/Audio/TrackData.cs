// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    [Serializable, JsonObject(MemberSerialization.OptOut)]
    internal class TrackData
    {
        [JsonIgnore] private static readonly List<IStreamResolver> Resolvers = new List<IStreamResolver>
        {
            new YoutubeResolver(),
            new SoundcloudResolver(),
            new LivestreamerResolver()
        };

        public string Location { get; }
        public TimeSpan Length { get; private set; }
        public string Name { get; private set; }

        [JsonConstructor, UsedImplicitly]
        private TrackData(string location, TimeSpan length, string name)
        {
            Location = location;
            Length = length;
            Name = name;
        }

        internal TrackData(string location)
        {
            Location = location;
            ReadLenght();
        }

        internal TrackData(string location, string name) : this(location)
        {
            Name = name;
        }

        internal static TrackData Create(string location)
        {
            if (File.Exists(location))
                return new TrackData(location, location.GetFilename());

            foreach (IStreamResolver res in Resolvers)
            {
                if (res.CanResolve(location))
                {
                    TrackData resolveResult = res.Resolve(location);
                    if (resolveResult != null) return resolveResult;
                }
            }
            return null;
        }

        private void ReadLenght()
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
}
