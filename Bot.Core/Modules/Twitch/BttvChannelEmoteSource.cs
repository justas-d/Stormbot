using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormbot.Bot.Core.Services;

namespace Stormbot.Bot.Core.Modules.Twitch
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BttvChannelEmoteSource : EmoteSourceBase
    {
        private static string DataSource = "https://api.betterttv.net/2/channels/";

        [JsonProperty]
        public string Channel { get; set; }

        private static readonly Regex ChannelNameValidatorRegex = new Regex(@"^[a-zA-Z0-9_]{4,25}");

        [JsonConstructor]
        private BttvChannelEmoteSource(string channel)
        {
            Channel = channel;
        }

        private BttvChannelEmoteSource(string channel, JObject data) : this(channel)
        {
            PopulateDictionary(data);
        }

        public static async Task<BttvChannelEmoteSource> Create(DiscordClient client, string channel)
        {
            try
            {
                if (!ChannelNameValidatorRegex.IsMatch(channel))
                    return null;

                HttpContent content =
                    await client.GetService<HttpService>().Send(HttpMethod.Get, $"{DataSource}{channel}");
                JObject result = JObject.Parse(await content.ReadAsStringAsync());

                if (result.GetValue("status").ToObject<int>() != 200)
                    return null;

                return new BttvChannelEmoteSource(channel, result);
            }
            catch (HttpException)
            {
            }
            return null;
        }

        public override Task DownloadData(HttpService http)
            => null; // k

        protected override void PopulateDictionary(JObject data)
        {
            foreach (JToken token in data["emotes"])
                EmoteDict.Add(token["code"].ToObject<string>(), token["id"].ToObject<string>());
        }

        public override async Task<string> GetEmote(string emote, HttpService http)
        {
            if (!EmoteDict.ContainsKey(emote))
                return null;

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
    }
}