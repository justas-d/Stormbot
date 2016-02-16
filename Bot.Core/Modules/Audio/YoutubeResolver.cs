using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrmyCore;
using YoutubeExtractor;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public sealed class YoutubeResolver : IStreamResolver
    {
        private async Task<VideoInfo> GetVideo(string input)
        {
            VideoInfo video = (await GetDownloadUrlsAsync(input))
                .OrderByDescending(v => v.AudioBitrate)
                .FirstOrDefault();

            if (video == null)
            {
                Logger.FormattedWrite(GetType().Name, "A video stream we could use wasn't found.");
                return null;
            }
            return video;
        }

        private static Task<IEnumerable<VideoInfo>> GetDownloadUrlsAsync(string videoUrl, bool decryptSignature = true)
            => Task.Run(() => DownloadUrlResolver.GetDownloadUrls(videoUrl, decryptSignature));

        bool IStreamResolver.SupportsTrackNames => true;
        bool IStreamResolver.SupportsAsyncCanResolve => false;

        async Task<string> IStreamResolver.ResolveStreamUrl(string input)
        {
            VideoInfo video = await GetVideo(input);

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            return video.DownloadUrl;
        }

        Task<bool> IStreamResolver.AsyncCanResolve(string input)
        {
            throw new NotSupportedException();
        }

        bool IStreamResolver.SyncCanResolve(string input)
        {
            string dummy;
            return DownloadUrlResolver.TryNormalizeYoutubeUrl(input, out dummy);
        }

        async Task<string> IStreamResolver.GetTrackName(string input)
            => (await GetVideo(input)).Title;
    }
}