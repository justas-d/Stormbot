// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using Newtonsoft.Json.Linq;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class SoundcloudResolver : IStreamResolver
    {
        internal static string ApiKey;

        public TrackData Resolve(string input)
        {
            if (ApiKey == null)
            {
                Logger.FormattedWrite(GetType().Name, "Soundcloud API Key was not set.");
                return null;
            }
            try
            {
                // make the api request.
                dynamic responce =
                    JObject.Parse(
                        Utils.DownloadRaw($"http://api.soundcloud.com/tracks/{GetPermalink(input)}?client_id={ApiKey}"));

                return new TrackData($"{((string) responce.stream_url)}?client_id={ApiKey}", (string) responce.title);
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed resolving soundcloud url. Ex: {ex}", ConsoleColor.Red);
                return null;
            }
        }

        private string GetPermalink(string input)
        {
            if (input.EndsWith("/"))
                input = input.Remove(input.Length - 1);
            input = input.Replace("https://", "");
            return input.Substring(input.LastIndexOf('/') + 1);
        }

        public bool CanResolve(string input) => input.Contains("soundcloud.com");
    }
}
