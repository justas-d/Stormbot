using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class Inventory
    {
        private Item[] _items;

        [JsonProperty]
        public int Size { get; }

        [JsonProperty]
        public Item[] Items => _items ?? (_items = new Item[Size]);

        [JsonConstructor, UsedImplicitly]
        private Inventory(int size, Item[] items)
        {
            Size = size;
            _items = items;
        }

        public Inventory(int size)
        {
            Size = size;
            _items = new Item[size];
        }

        /// <summary>
        /// Tries to add the given amount of item to the inventory.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        /// <returns>True if we added the given amount of items succesfully, false if otherwise.</returns>
        public bool AddItem(Item item)
        {
            for (int i = 0; i < item.Amount; i++)
            {
                if (!AddItem(item.ItemDef))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tries to add the given amount of item (found by it's id) to the inventory.
        /// </summary>
        /// <param name="id">The id of the item which will be added.</param>
        /// <param name="amount">The amount of the item.</param>
        /// <returns>True if we added the given amount of items succesfully, false if otherwise.</returns>
        public bool AddItem(uint id, uint amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!AddItem(ItemDef.Get(id)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tries to add the give item to the inventory.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        /// <returns>True if we added the given item succesfully, false if otherwise.</returns>
        public bool AddItem(ItemDef item)
        {
            // stack
            if (item.CanStack)
            {
                for (int i = 0; i < Size; i++)
                {
                    //skip if the slot isin't null.
                    if (Items[i] == null) continue;

                    // check if the itemstack can hold more 
                    //and if it can check if it's the same as the one we are trying to add.
                    if (!Items[i].CanHoldMore || Items[i].Id != item.Id) continue;

                    Items[i].Amount++;
                    return true;
                }
            }
            //Either we stacked or we didnt, but we add the item in a new slot now.
            for (int i = 0; i < Size; i++)
            {
                if (Items[i] != null) continue;

                Items[i] = new Item(item.Id, 1);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < Size; i++)
            {
                builder.Append($"{i + 1,-2}: ");
                builder.AppendLine(Items[i] != null ?
                    $"{Items[i].ItemDef.Name,-15} x{Items[i].Amount}." :
                    $"Empty.");
            }

            return builder.ToString();
        }
    }
}
