using System;
using System.Linq;
using System.Threading.Tasks;
using StrmyCore;
using YoutubeExtractor;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public sealed class YoutubeResolver : IStreamResolver
    {
        public async Task<string> ResolveStreamUrl(string input)
        {
            VideoInfo video = GetVideo(input);

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            return video.DownloadUrl;
        }

        // todo : this too
        public async Task<string> GetTrackName(string input) => GetVideo(input).Title;

        private VideoInfo GetVideo(string input)
        {
            VideoInfo video = DownloadUrlResolver.GetDownloadUrls(input)
                .OrderByDescending(v => v.AudioBitrate)
                .FirstOrDefault();

            if (video == null)
            {
                Logger.FormattedWrite(GetType().Name, "A video stream we could use wasn't found.");
                return null;
            }
            return video;
        }

        public bool CanResolve(string input)
        {
            string dummy;
            return DownloadUrlResolver.TryNormalizeYoutubeUrl(input, out dummy);
        }
    }
}