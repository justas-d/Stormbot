using System;
using System.Linq;
using YoutubeExtractor;

namespace Stormbot.Bot.Core.Modules.Audio
{
    public sealed class YoutubeResolver : IStreamResolver
    {
        public string ResolveStreamUrl(string input)
        {
            VideoInfo video = GetVideo(input);

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            return video.DownloadUrl;
        }

        public string GetTrackName(string input) => GetVideo(input).Title;

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