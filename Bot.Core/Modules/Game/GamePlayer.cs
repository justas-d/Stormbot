using System;
using System.Text;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GamePlayer
    {
        public enum GenderType
        {
            Male,
            Female,
            Yes
        }

        public const int DefaultInventorySize = 28;
        public const int DefaultMaxHealth = 100;

        private Inventory _inventory;
        private Bank _bank;
        private int _health = DefaultMaxHealth;

        /// <summary> Gets or sets the players location. 
        /// Use Location.Enter to "Enter" the location properly instead of setting it manually here. 
        /// Setting it manually can lead to unintended behaviour.</summary>
        public Location Location { get; set; }

        #region Serialization Objects

        [JsonProperty]
        public int MaxHealth { get; set; } = DefaultMaxHealth;

        [JsonProperty]
        public int Health
        {
            get { return _health; }
            set
            {
                _health = value > MaxHealth
                    ? MaxHealth
                    : value;

                if (_health <= 0)
                    OnDead();
            }
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

        [JsonProperty]
        public Bank Bank => _bank ?? (_bank = new Bank());

        [JsonProperty]
        public uint LocationId => Location.Id;

        #endregion

        public User User { get; set; }

        public bool IsDead => Health <= 0;

        ///<summary>Returns whether the player is still in the character creation process.</summary>
        public bool IsInCharacterCreation => Name == null || CreateState != null;

        [JsonConstructor]
        private GamePlayer(string name, ulong userid, GenderType gender, PlayerCreateState createState,
            Inventory inventory, uint locationId, Bank bank, int health, int maxhealth)
        {
            Name = name;
            UserId = userid;
            Gender = gender;
            _inventory = inventory;
            Health = health;
            MaxHealth = maxhealth;

            if(MaxHealth == 0)

            _bank = bank;

            Location = Location.Get(locationId);

            CreateState = createState;
            if (CreateState != null) // player is still in the creation process.
                CreateState.Player = this;
        }

        public GamePlayer(ulong userid, User user = null)
        {
            UserId = userid;
            User = user;
            CreateState = new PlayerCreateState(this);
        }

        public void FinishPlayerCreate()
        {
            Name = CreateState.Name;
            Gender = CreateState.Gender;
            CreateState = null;
            Location = Location.Get();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int) UserId*13;
            }
        }

        private void OnDead()
        {
            
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(IsInCharacterCreation
                ? CreateState.ToString()
                : $"Name: {Format.Code(Name)}\r\nHealth: {Format.Code($"{Health}/{MaxHealth}")}\r\nGender: {Format.Code(Gender.ToString())}\r\nLocation: {Format.Code(Location.Name)}.\r\n ```{Location}```");

            return builder.ToString();
        }
    }
}