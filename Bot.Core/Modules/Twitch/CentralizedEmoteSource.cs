using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    public abstract class EmoteSourceBase
    {
        protected readonly Dictionary<string, string> EmoteDict = new Dictionary<string, string>();
        public abstract Task DownloadData(HttpService http);
        public abstract Task<string> GetEmote(string emote, HttpService http);
        protected abstract void PopulateDictionary(JObject data);
        public bool ContainsEmote(string emote)
            => EmoteDict.ContainsKey(emote);
    }

    public abstract class CentralziedEmoteSource : EmoteSourceBase
    {
        protected abstract string DataSource { get; }

        public override async Task DownloadData(HttpService http)
        {
            try
            {
                Logger.FormattedWrite(GetType().Name, $"Fetching emote data for {GetType().Name}", ConsoleColor.Green);

                HttpContent content = await http.Send(HttpMethod.Get, DataSource);
                PopulateDictionary(JObject.Parse(await content.ReadAsStringAsync()));
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite(GetType().Name, $"Failed fetching emote data. Ex: {ex}", ConsoleColor.Red);
            }
        }
    }
}