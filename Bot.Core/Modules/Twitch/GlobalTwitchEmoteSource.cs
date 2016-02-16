using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    internal class GlobalTwitchEmoteSource : CentralizedEmoteSource
    {
        public override string DataSource => "https://api.twitch.tv/kraken/chat/emoticons";
        public override string CachedFileName => "globaltwitch.json";

        protected override void PopulateDictionary(JObject data)
        {
            foreach (JToken token in data["emoticons"])
            {
                EmoteDict.Add(token["regex"].ToObject<string>(),
                    ((JArray) token["images"]).Children().First()["url"].ToObject<string>());
            }

            Logger.FormattedWrite(
                GetType().Name,
                $"Loaded {EmoteDict.Count} global twitch emotes.",
                ConsoleColor.DarkGreen);
        }

        public override async Task<string> GetEmote(string emote, HttpService http)
        {
            try
            {
                string url = EmoteDict[emote];
                string dir = Path.Combine(Constants.TwitchEmoteFolderDir, emote + ".png");

                if (!File.Exists(dir))
                {
                    HttpContent content = await http.Send(HttpMethod.Get, url);
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
            => EmoteDict.ContainsKey(emote);
    }
}