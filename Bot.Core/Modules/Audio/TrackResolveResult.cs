// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal class TrackResolveResult
    {
        public TrackData Track { get; set; }
        public bool WasSuccessful { get; set; }
        public string Message { get; set; }

        public TrackResolveResult()
        {
            
        }

        public TrackResolveResult(TrackData track, bool wasSuccessful, string message = "")
        {
            Track = track;
            WasSuccessful = wasSuccessful;
            Message = message;
        }
    }
}
