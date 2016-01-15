using System;
using Newtonsoft.Json.Linq;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class SoundcloudResolver : IStreamResolver
    {
        internal static string ApiKey;

        public string ResolveStreamUrl(string input)
        {
            if (ApiKey != null) return (string) GetTrackData(GetPermalink(input)).stream_url;

            Logger.FormattedWrite(GetType().Name, "Soundcloud API Key was not set.");
            return null;
        }

        public bool CanResolve(string input) => input.Contains("soundcloud.com");
        public string GetTrackName(string input) => GetTrackData(GetPermalink(input)).title;

        private dynamic GetTrackData(string permalink)
        {
            try
            {
                // make the api request.
                return JObject.Parse(
                    Utils.DownloadRaw($"http://api.soundcloud.com/tracks/{permalink}?client_id={ApiKey}"));
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed getting sc track data. Ex: {ex}", ConsoleColor.Red);
                return null;
            }
        }

        private string GetPermalink(string input)
        {
            try
            {
                if (input.EndsWith("/"))
                    input = input.Remove(input.Length - 1);
                input = input.Replace("https://", "");
                return input.Substring(input.LastIndexOf('/') + 1);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
