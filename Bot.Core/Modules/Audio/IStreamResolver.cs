using System.Threading.Tasks;

namespace Stormbot.Bot.Core.Modules.Audio
{
    internal interface IStreamResolver
    {
        bool SupportsTrackNames { get; }
        bool SupportsAsyncCanResolve { get; }

        /// <summary> Attempts to resolve and return a content stream url from the given input. </summary>
        Task<string> ResolveStreamUrl(string input);

        /// <summary> Asyncronously returns whether this resolver can resolve the given input.</summary>
        Task<bool> AsyncCanResolve(string input);

        /// <summary> Synchronously returns whether this resolver can resolve the given input.</summary>
        bool SyncCanResolve(string input);

        /// <summary> Returns the name of the given track. </summary>
        Task<string> GetTrackName(string input);
    }
}