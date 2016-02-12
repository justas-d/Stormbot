using Discord;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class JsonChannel
    {
        private Channel _channel;

        public Channel Channel
        {
            get { return _channel; }
            set
            {
                _channel = value;
                ChannelId = value.Id;
            }
        }

        [JsonProperty]
        public ulong ChannelId { get; private set; }

        [JsonConstructor]
        private JsonChannel(ulong channelId)
        {
            ChannelId = channelId;
        }

        public JsonChannel(Channel channel)
        {
            Channel = channel;
        }

        public static implicit operator JsonChannel(Channel channel)
            => new JsonChannel(channel);

        public static implicit operator Channel(JsonChannel channel)
            => channel.Channel;

        public bool FinishLoading(DiscordClient client)
        {
            Channel channel = client.GetChannel(ChannelId);
            if (channel == null)
                return false;

            Channel = channel;
            return true;
        }
    }
}
