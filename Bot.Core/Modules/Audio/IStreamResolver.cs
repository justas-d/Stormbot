// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal interface IStreamResolver
    {
        /// <summary> Attempts to resolve and return a content stream url from the given input. </summary>
        string ResolveStreamUrl(string input);

        /// <summary> Returns whether this resolver can resolve the given input.</summary>
        bool CanResolve(string input);

        /// <summary> Returns the name of the given track. </summary>
        string GetTrackName(string input);
    }
}