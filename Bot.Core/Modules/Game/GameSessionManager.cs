using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GameSessionManager
    {
        [JsonProperty] private HashSet<GamePlayer> _players = new HashSet<GamePlayer>();
        private HashSet<GamePlayer> Players => _players ?? (_players = new HashSet<GamePlayer>());

        [JsonConstructor]
        private GameSessionManager(HashSet<GamePlayer> players)
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

            Players.Add(new GamePlayer(user.Id, user));
            await user.SendPrivate("Welcome to the character creation! Use !help cc to find out what you can do.");
        }

        [CanBeNull]
        public GamePlayer GetPlayer(ulong userid) => Players.FirstOrDefault(p => p.UserId == userid);
        [CanBeNull]
        public GamePlayer GetPlayer(User user) => GetPlayer(user.Id);

        public bool PlayerExists(ulong userid) => GetPlayer(userid) != null;
    }
}
