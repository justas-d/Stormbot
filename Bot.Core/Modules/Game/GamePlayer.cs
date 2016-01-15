using System;
using Discord;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GamePlayer
    {
        public const int DefaultInventorySize = 28;

        private Inventory _inventory;

        public enum GenderType
        {
            Male,
            Female,
            Yes
        }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public ulong UserId { get; }

        [JsonProperty]
        public GenderType Gender { get; private set; }

        [JsonProperty]
        public PlayerCreateState CreateState { get; private set; }

        [JsonProperty]
        public Inventory Inventory => _inventory ?? (_inventory = new Inventory(DefaultInventorySize));

        private User _cachedUser;

        [JsonConstructor]
        private GamePlayer(string name, ulong userid, GenderType gender, PlayerCreateState createState, Inventory inventory)
        {
            Name = name;
            UserId = userid;
            Gender = gender;
            _inventory = inventory;

            CreateState = createState;
            if (CreateState != null) // player is still in the creation process.
                CreateState.Player = this;
        }

        public GamePlayer(ulong userid, User cachedUser = null)
        {
            UserId = userid;
            _cachedUser = cachedUser;
            CreateState = new PlayerCreateState(this);
        }

        public void FinishPlayerCreate()
        {
            Name = CreateState.Name;
            Gender = CreateState.Gender;
            CreateState = null;
        }

        [CanBeNull]
        public User GetUser(DiscordClient client) => _cachedUser ?? (_cachedUser = client.GetUser(UserId));

        public override int GetHashCode()
        {
            unchecked
            {
                return (int) UserId*13;
            }
        }

        public override string ToString() => $"Name: {Format.Code(Name)}\r\nGender: {Format.Code(Gender.ToString())}";
    }
}