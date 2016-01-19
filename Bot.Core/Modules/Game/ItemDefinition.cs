using System;
using System.Collections.Generic;

namespace Stormbot.Bot.Core.Modules.Game
{
    public class ItemDef
    {
        public static readonly Dictionary<uint, ItemDef> Dict = new Dictionary<uint, ItemDef>();

        public uint Id { get; }
        public string Name { get; }
        public string Description { get; }

        public int MaxStack { get; } = 99;
        public bool CanStack { get; }

        private ItemDef(uint id, string name, string desc, bool canStack)
        {
            Id = id;
            Name = name;
            Description = desc;
            CanStack = canStack;

            if (Dict.ContainsKey(id))
            {
                Logger.FormattedWrite(GetType().Name, $"An item with the id {id} already exists.", ConsoleColor.Red);
                return;
            }

            Dict.Add(id, this);
        }

        ///<summary>Tries to get an item definition from the item definition dictionary.</summary>
        public static ItemDef Get(uint id) => Dict[id];

        static ItemDef()
        {
            new ItemDef(0, "Book", "A fucking book", true);
            new ItemDef(1, "Clay Pot", "A pot, made out of clay.", true);
            new ItemDef(2, "Fedora", "M'lady", false);
            new ItemDef(3, "Coin", "Lovely money!", true);
        }

        #region Overrides

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Id * 13;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ItemDef p = obj as ItemDef;
            if ((object)p == null)
                return false;

            return Id == p.Id;
        }

        public bool Equals(ItemDef p)
        {
            if ((object)p == null)
                return false;

            return Id == p.Id;
        }

        public static bool operator ==(ItemDef a, ItemDef b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Id == b.Id;
        }

        public static bool operator !=(ItemDef a, ItemDef b)
        {
            return !(a == b);
        }

        #endregion
    }
}
