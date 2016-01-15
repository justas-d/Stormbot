using System;
using System.Text;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class PlayerCreateState
    {
        /// <summary> Returns whether the player can finish creating this character. </summary>
        public bool CanFinishCreation => !string.IsNullOrEmpty(Name);

        public GamePlayer Player { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public GamePlayer.GenderType Gender { get; set; }

        [JsonConstructor]
        private PlayerCreateState(string name, GamePlayer.GenderType gender)
        {
            Name = name;
            Gender = gender;
        }

        public PlayerCreateState(GamePlayer player)
        {
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("`");

            if (!string.IsNullOrEmpty(Name))
                builder.AppendLine($"Name: {Name}");

            builder.AppendLine($"Gender: {Gender}");

            return $"{builder}`";
        }
    }
}
