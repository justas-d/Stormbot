using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class SoundcloudResolver : IStreamResolver
    {
        internal static string ApiKey;
        private readonly Dictionary<string, dynamic> _cachedTrackData = new Dictionary<string, dynamic>();
        private string ClientIdParam => $"client_id={ApiKey}";

        private async Task<dynamic> GetTrackData(string trackUrl)
        {
            try
            {
                if (_cachedTrackData.ContainsKey(trackUrl))
                    return _cachedTrackData[trackUrl];

                dynamic trackCallback =
                    JObject.Parse(
                        await
                            Utils.AsyncDownloadRaw($"http://api.soundcloud.com/resolve?url={trackUrl}&{ClientIdParam}"));

                _cachedTrackData.Add(trackUrl, trackCallback);

                return trackCallback;
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed getting sc track data. Ex: {ex}", ConsoleColor.Red);
                return null;
            }
        }

        bool IStreamResolver.SupportsTrackNames => true;
        bool IStreamResolver.SupportsAsyncCanResolve => false;

        async Task<string> IStreamResolver.ResolveStreamUrl(string input)
        {
            if (ApiKey != null)
                return $"{(string) (await GetTrackData(input)).stream_url}?{ClientIdParam}";

            Logger.FormattedWrite(GetType().Name, "Soundcloud API Key was not set.");
            return null;
        }

        Task<bool> IStreamResolver.AsyncCanResolve(string input)
        {
            throw new NotSupportedException();
        }

        bool IStreamResolver.SyncCanResolve(string input) => input.Contains("soundcloud.com");
        async Task<string> IStreamResolver.GetTrackName(string input) => (await GetTrackData(input)).title;
    }
}