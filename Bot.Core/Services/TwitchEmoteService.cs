// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Stormbot.Helpers;
using StrmyCore;

namespace Stormbot.Bot.Core.Services
{
    public class TwitchEmoteService : IService
    {
        private HttpService _http;

        private readonly List<CentralizedEmoteSource> _emoteSources = new List<CentralizedEmoteSource>
        {
            new GlobalTwitchEmoteSource(),
            new BttvEmoteSource(),
        };

        public void Install(DiscordClient client)
        {
            _http = client.Services.Get<HttpService>();

            foreach (ITwitchEmoteSource source in _emoteSources)
            {
                try
                {
                    source.FetchData(_http);
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite(
                        GetType().Name,
                        $"Failed loading emotes for {source.GetType().Name}. Exception: {ex}");
                }
            }
            GC.Collect();
        }

        public async Task<string> GetRandomEmote()
        {
            CentralizedEmoteSource source = _emoteSources.PickRandom();
            return await source.GetEmote(source.EmoteDict.Keys.PickRandom(), _http);
        }

        public async Task<string> ResolveEmoteDir(string userInput)
        {
            foreach (ITwitchEmoteSource source in _emoteSources)
                if (source.ContainsEmote(userInput)) return await source.GetEmote(userInput, _http);

            return null;
        }
    }

    internal interface ITwitchEmoteSource
    {
        string DataSource { get; }
        string CachedFileName { get; }

        Task FetchData(HttpService http);

        [CanBeNull]
        Task<string> GetEmote(string emote, HttpService http);

        bool ContainsEmote(string emote);
    }

    internal abstract class CentralizedEmoteSource : ITwitchEmoteSource
    {
        public abstract string DataSource { get; }
        public abstract string CachedFileName { get; }

        public readonly Dictionary<string, string> EmoteDict = new Dictionary<string, string>();

        public virtual async Task FetchData(HttpService http)
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

        protected abstract void PopulateDictionary(JObject data);
        public abstract Task<string> GetEmote(string emote, HttpService http);
        public abstract bool ContainsEmote(string emote);
    }

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
        {
            return EmoteDict.ContainsKey(emote);
        }
    }

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
                    HttpContent content = await http.Send(HttpMethod.Get, $"https://cdn.betterttv.net/emote/{imageId}/2x");
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