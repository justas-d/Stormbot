// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal interface IStreamResolver
    {
        TrackData Resolve(string input);
        bool CanResolve(string input);
    }
}
