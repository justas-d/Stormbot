using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class SoundcloudResolver : IStreamResolver
    {
        private readonly Dictionary<string, dynamic> _cachedTrackData = new Dictionary<string, dynamic>();

        internal static string ApiKey;
        private string ClientIdParam => $"?client_id={ApiKey}";

        public string ResolveStreamUrl(string input)
        {
            if (ApiKey != null)
                return $"{(string) GetTrackData(input).stream_url}{ClientIdParam}";

            Logger.FormattedWrite(GetType().Name, "Soundcloud API Key was not set.");
            return null;
        }

        public bool CanResolve(string input) => input.Contains("soundcloud.com");
        public string GetTrackName(string input) => GetTrackData(input).title;

        private dynamic GetTrackData(string trackUrl)
        {
            try
            {
                if (_cachedTrackData.ContainsKey(trackUrl))
                    return _cachedTrackData[trackUrl];

                dynamic trackCallback =
                    JObject.Parse(
                        Utils.DownloadRaw($"http://api.soundcloud.com/resolve?url={trackUrl}&client_id={ApiKey}"));

                _cachedTrackData.Add(trackUrl, trackCallback);

                return trackCallback;
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed getting sc track data. Ex: {ex}", ConsoleColor.Red);
                return null;
            }
        }
    }
}
