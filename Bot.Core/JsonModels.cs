using Discord;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class JosnChannel
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
        private JosnChannel(ulong channelId)
        {
            ChannelId = channelId;
        }
       
        public JosnChannel(Channel channel)
        {
            Channel = channel;
        }

        public static implicit operator JosnChannel(Channel channel)
            => new JosnChannel(channel);

        public void FinishLoading(DiscordClient client) => Channel = client.GetChannel(ChannelId);
    }
}
