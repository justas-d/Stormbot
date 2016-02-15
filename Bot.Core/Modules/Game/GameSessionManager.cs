using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GameSessionManager
    {
        [JsonProperty] private Dictionary<ulong, GamePlayer> _players = new Dictionary<ulong, GamePlayer>();
        public Dictionary<ulong, GamePlayer> Players => _players ?? (_players = new Dictionary<ulong, GamePlayer>());

        [JsonConstructor]
        private GameSessionManager(Dictionary<ulong, GamePlayer> players)
        {
            _players = players;
        }

        public GameSessionManager()
        {
        }

        public async Task Join(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (PlayerExists(user.Id))
            {
                await user.SendPrivate("You're already playing.");
                return;
            }

            GamePlayer player = new GamePlayer(user.Id, user);
            Players.Add(user.Id, player);
            await
                player.User.SendPrivate("Welcome to the character creation! Use !help cc to find out what you can do.");
        }

        public GamePlayer GetPlayer(ulong userid) => Players.TrySafeGet(userid, false);

        public GamePlayer GetPlayer(User user) => GetPlayer(user.Id);

        public bool PlayerExists(ulong userid) => Players.ContainsKey(userid);
    }
}