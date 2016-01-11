// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System.Linq;
using YoutubeExtractor;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal sealed class YoutubeResolver : IStreamResolver
    {
        public TrackResolveResult Resolve(string input)
        {
            TrackResolveResult retval = new TrackResolveResult();

            if (!CanResolve(input))
            {
                retval.Message = "Input wasn't a valid youtube url.";
                return retval;
            }

            VideoInfo video = DownloadUrlResolver.GetDownloadUrls(input)
                .OrderByDescending(v => v.AudioBitrate)
                .FirstOrDefault();

            if (video == null)
            {
                retval.Message = "A video stream we could use wasn't found.";
                return retval;
            }

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            retval.Track = new TrackData(video.DownloadUrl, video.Title);
            retval.WasSuccessful = true;
            return retval;
        }

        public bool CanResolve(string input)
        {
            string dummy;
            return DownloadUrlResolver.TryNormalizeYoutubeUrl(input, out dummy);
        }
    }
}
