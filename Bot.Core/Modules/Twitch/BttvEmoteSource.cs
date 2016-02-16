using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    internal class BttvEmoteSource : CentralizedEmoteSource
    {
        public override string DataSource => "https://api.betterttv.net/2/emotes";
        public override string CachedFileName => "bttv.json";

        protected override void PopulateDictionary(JObject data)
        {
            foreach (JToken token in data["emotes"])
                EmoteDict.Add(token["code"].ToObject<string>(), token["id"].ToObject<string>());

            Logger.FormattedWrite(GetType().Name, $"Loaded {EmoteDict.Count} bttv emotes.", ConsoleColor.DarkGreen);
        }

        public override async Task<string> GetEmote(string emote, HttpService http)
        {
            try
            {
                string imageId = EmoteDict[emote];
                string dir = Path.Combine(Constants.TwitchEmoteFolderDir, imageId + ".png");

                if (!File.Exists(dir))
                {
                    HttpContent content =
                        await http.Send(HttpMethod.Get, $"https://cdn.betterttv.net/emote/{imageId}/2x");
                    File.WriteAllBytes(dir, await content.ReadAsByteArrayAsync());
                }

                return dir;
            }
            catch (KeyNotFoundException)
            {
            } //ignored
            return null;
        }

        public override bool ContainsEmote(string emote)
        {
            return EmoteDict.ContainsKey(emote);
        }
    }
}