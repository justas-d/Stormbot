using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    [Serializable, JsonObject(MemberSerialization.OptOut)]
    public class TrackData
    {
        [JsonIgnore] private static readonly List<IStreamResolver> Resolvers = new List<IStreamResolver>
        {
            new YoutubeResolver(),
            new SoundcloudResolver(),
            new LivestreamerResolver()
        };

        // cached reference to a valid resolver, we don't serialzie this.
        [JsonIgnore] private IStreamResolver _cachedResolver;
        public string Location { get; }
        public TimeSpan? Length { get; set; }
        public string Name { get; private set; }

        [JsonConstructor]
        private TrackData(string location, TimeSpan? length, string name)
        {
            Location = location;
            Length = length;
            Name = name;
        }

        private TrackData(string location, string name)
        {
            Location = location;
            Name = name;
        }

        private TrackData(string location, IStreamResolver resovler)
        {
            Location = location;
            _cachedResolver = resovler;
        }

        private async Task<string> GetStreamUrl()
        {
            if (File.Exists(Location)) return Location;

            if (_cachedResolver != null)
                return await _cachedResolver.ResolveStreamUrl(Location);

            IStreamResolver resolver = await FindValidResovler(Location);

            if (resolver == null)
                return null;

            return await resolver.ResolveStreamUrl(Location);
        }

        private static async Task<IStreamResolver> FindValidResovler(string location)
        {
            IStreamResolver resolver = null;

            foreach (IStreamResolver candidateResolver in Resolvers)
            {
                if (candidateResolver.SupportsAsyncCanResolve)
                {
                    if (await candidateResolver.AsyncCanResolve(location))
                    {
                        resolver = candidateResolver;
                        break;
                    }
                }
                else if (candidateResolver.SyncCanResolve(location))
                {
                    resolver = candidateResolver;
                    break;
                }
            }

            return resolver;
        }

        public async Task<string> GetStream()
        {
            string stream = await GetStreamUrl();

            if (string.IsNullOrEmpty(stream))
            {
                Logger.FormattedWrite("TrackData", $"Failed getting stream url for {Location}", ConsoleColor.Red);
                return stream;
            }

            if (Length == null)
                await GetLength(stream);

            return stream;
        }

        public static async Task<TrackData> Parse(string input)
        {
            if (File.Exists(input))
                return new TrackData(input, Utils.GetFilename(input));

            IStreamResolver resolver = await FindValidResovler(input);

            if (resolver == null)
                return null;

            TrackData retval = new TrackData(input, resolver);

            if (resolver.SupportsTrackNames)
                retval.Name = await resolver.GetTrackName(input);
            else
                retval.Name = input;

            return retval;
        }

        private Task GetLength(string location)
            => Task.Run(() =>
            {
                try
                {
                    using (Process ffprobe = new Process
                    {
                        StartInfo =
                        {
                            FileName = Constants.FfprobeDir,
                            Arguments =
                                $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{location}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        },
                        EnableRaisingEvents = true
                    })
                    {
                        ffprobe.OutputDataReceived += (sender, args) =>
                        {
                            if (string.IsNullOrEmpty(args.Data)) return;
                            if (args.Data == "N/A") return;

                            Length = TimeSpan.FromSeconds(
                                int.Parse(
                                    args.Data.Remove(
                                        args.Data.IndexOf('.'))));
                        };

                        if (!ffprobe.Start())
                            Logger.FormattedWrite(typeof (TrackData).Name, "Failed starting ffprobe.", ConsoleColor.Red);

                        ffprobe.BeginOutputReadLine();
                        ffprobe.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite(typeof (TrackData).Name, $"Failed getting track length. Exception: {ex}",
                        ConsoleColor.Red);
                }
            });
    }
}