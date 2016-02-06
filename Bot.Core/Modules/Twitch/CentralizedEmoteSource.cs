using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    internal abstract class CentralizedEmoteSource
    {
        public abstract string DataSource { get; }
        public abstract string CachedFileName { get; }

        public readonly Dictionary<string, string> EmoteDict = new Dictionary<string, string>();

        public virtual async Task FetchData(HttpService http)
        {
            try
            {
                string fileDir = Path.Combine(Constants.TwitchEmoteFolderDir, CachedFileName);

                if (!File.Exists(fileDir))
                {
                    // download the data if it doesn't exist.
                    Logger.FormattedWrite(
                        GetType().Name,
                        $"Fetching emote data for {GetType().Name}",
                        ConsoleColor.Green);

                    HttpContent content = await http.Send(HttpMethod.Get, DataSource);
                    File.WriteAllText(fileDir, await content.ReadAsStringAsync());
                }
                PopulateDictionary(JObject.Parse(File.ReadAllText(fileDir)));
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed fetching emote data. Ex: {ex}", ConsoleColor.Red);
            }
        }

        protected abstract void PopulateDictionary(JObject data);
        public abstract Task<string> GetEmote(string emote, HttpService http);
        public abstract bool ContainsEmote(string emote);
    }
}
