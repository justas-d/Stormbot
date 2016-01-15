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

        public int MaxStack { get; set; } = 99;
        public bool CanStack = true;

        private ItemDef(uint id, string name, string desc)
        {
            Id = id;
            Name = name;
            Description = desc;

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
            new ItemDef(0, "Book", "A fucking book");
            new ItemDef(1, "Clay Pot", "A pot, made out of clay.");
            new ItemDef(2, "Fedora", "M'lady");
        }
    }
}
