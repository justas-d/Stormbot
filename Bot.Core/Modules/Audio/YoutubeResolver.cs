// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System.Linq;
using YoutubeExtractor;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class YoutubeResolver : IStreamResolver
    {
        public TrackData Resolve(string input)
        {
            if (!CanResolve(input)) return null;

            VideoInfo video = DownloadUrlResolver.GetDownloadUrls(input)
                .OrderByDescending(v => v.AudioBitrate)
                .FirstOrDefault();

            if (video == null)
            {
                Logger.FormattedWrite(GetType().Name, "A video stream we could use wasn't found.");
                return null;
            }

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            return new TrackData(video.DownloadUrl, video.Title);
        }

        public bool CanResolve(string input)
        {
            string dummy;
            return DownloadUrlResolver.TryNormalizeYoutubeUrl(input, out dummy);
        }
    }
}
