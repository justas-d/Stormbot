using System;
using Newtonsoft.Json;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class Item
    {
        public ItemDef ItemDef { get; }

        private int _amount;

        ///<summary>Returns much of the same item we can add to the stack.</summary>
        public int CanHoldAmount => ItemDef.MaxStack - Amount;

        ///<summary>Returns wheteher we add more to the stack.</summary>
        public bool CanHoldMore => CanHoldAmount > 0;

        [JsonProperty]
        public uint Id => ItemDef.Id;

        [JsonProperty]
        public int Amount
        {
            get { return _amount; }
            set
            {
                if (value < 0) _amount = 0;
                else if (value > ItemDef.MaxStack) _amount = ItemDef.MaxStack;
                else _amount = value;
            }
        }

        [JsonConstructor]
        public Item(uint id, int amount)
        {
            ItemDef = ItemDef.Dict.TrySafeGet(id);
            Amount = amount;
        }
    }
}
