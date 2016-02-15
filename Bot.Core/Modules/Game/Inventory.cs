using System;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace Stormbot.Bot.Core.Modules.Game
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class Inventory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected Item[] _items;

        ///<summary>Gets whether this inventory has space for more items, including stacking items.</summary>
        public bool HasSpace
        {
            get
            {
                foreach (Item item in Items)
                {
                    // check if the item even exists.
                    if (item != null)
                    {
                        if (item.CanHoldMore) // check if we can stack more items on the item.
                            return true; // if we can then woo we have more space.
                    } // if it doesn't that means we can add a new item in place, return true.
                    else return true;
                }
                return false;
            }
        }

        [JsonProperty]
        public int Size { get; }

        [JsonProperty]
        public Item[] Items => _items ?? (_items = new Item[Size]);

        [JsonConstructor]
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
        /// Checks whether this inventory can hold the given itemdef item once.
        /// </summary>
        /// <param name="itemdef">The itemdef which will be used when checking.</param>
        /// <returns>True if this inventory can hold one of the itemdef, false if otherwise.</returns>
        public bool CanHoldItem(ItemDef itemdef)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                // null means we can put the itemdef in place of the null.
                if (Items[i] == null)
                    return true;

                if (!itemdef.CanStack) continue;

                // check for stacking
                if (Items[i].Id == itemdef.Id)
                    return Items[i].CanHoldMore; // return true if we can stack more items on top of the current index.
            }
            return false;
        }

        /// <summary>
        /// Checks whether this inventory can hold the given itemdef for the given amount.
        /// </summary>
        /// <param name="itemdef">The itemdef which will be used when checking.</param>
        /// <param name="amount">The amount of the itemdef.</param>
        /// <returns>True if we can hold the amount of itemdef in the inventory, false if otherwise.</returns>
        public bool CanHoldItem(ItemDef itemdef, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!CanHoldItem(itemdef))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether this inventory can hold the given item.
        /// </summary>
        /// <param name="item">The item which will be used when checking.</param>
        /// <returns>True if this inventory can hold this item, false if otherwise.</returns>
        public bool CanHoldItem(Item item) => CanHoldItem(item.ItemDef, item.Amount);

        /// <summary>
        /// Removes and returns the found item at the given index.
        /// </summary>
        /// <param name="index">The index of the item array from which we will remove and return an item</param>
        /// <returns>The item, found at given index.</returns>
        public Item TakeItem(int index)
        {
            // check for out of range input.
            if (index < 0 ||
                index >= Size) return null;

            Item retval = Items[index];
            Items[index] = null;

            return retval;
        }

        /// <summary>
        /// Tries to add the given amount of item to the inventory.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        /// <returns>True if we added the given amount of items succesfully, false if otherwise.</returns>
        public bool AddItem(Item item)
        {
            // check whether we can even hold the item
            if (!CanHoldItem(item)) return false;

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
            ItemDef itemdef = ItemDef.Get(id);

            // check whether we can even hold the item
            if (!CanHoldItem(itemdef)) return false;

            for (int i = 0; i < amount; i++)
            {
                if (!AddItem(itemdef))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to add an item to the inventory at the given index.
        /// </summary>
        /// <param name="item">The item which we will add.</param>
        /// <param name="index">The index in the inventory at which we will try to add the item at.</param>
        /// <returns>True if we added the item succesfully, false if otherwise.</returns>
        public bool AddItemIndex(Item item, int index)
        {
            // check for out of range input.
            if (index < 0 ||
                index >= Size) return false;

            Item itemAtIndex = Items[index];

            if (itemAtIndex == null) // no item at index, overwrite with our new item.
            {
                Items[index] = item;
                return true;
            }

            for (int i = 0; i < item.Amount; i++)
            {
                if (AddItemIndex(item.ItemDef, index))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to add an itemdef item to the inventory at the given index.
        /// </summary>
        /// <param name="item">The itemdef item which we will add one of.</param>
        /// <param name="index">The index in the item inventory at which we will try to add the itemdef.</param>
        /// <returns>True if we added the item succesfully, false if otherwise.</returns>
        public bool AddItemIndex(ItemDef item, int index)
        {
            // check for out of range input.
            if (index < 0 ||
                index >= Size) return false;

            Item itemAtIndex = Items[index];

            // try to force add the item
            if (itemAtIndex == null)
            {
                Items[index] = new Item(item.Id, index);
                return true;
            }

            // check for stacking
            if (itemAtIndex.Id == item.Id) // only try stacking items of the same id.
                if (itemAtIndex.CanHoldMore) // check if the item at index can hold more
                {
                    itemAtIndex.Amount++; // if it can, perfect. Stack it.
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Tries to add the give item to the inventory.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        /// <returns>True if we added the given item succesfully, false if otherwise.</returns>
        public bool AddItem(ItemDef item)
        {
            // check whether we can even hold the item
            if (!CanHoldItem(item)) return false;

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

        public override string ToString() => ItemInfo(0, Size);

        protected string ItemInfo(int startPos, int size)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = startPos; i < size; i++)
            {
                builder.Append($"{i + 1,-2}: ");
                builder.AppendLine(Items[i] != null
                    ? $"{Items[i].ItemDef.Name,-15} x{Items[i].Amount}."
                    : $"Empty.");
            }

            return builder.ToString();
        }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class Bank : Inventory
    {
        public const int ItemsPerPage = 50;
        public const int DefaultBankSize = 200;

        public static readonly int Pages = (int) Math.Ceiling((float) DefaultBankSize/ItemsPerPage);

        [JsonConstructor]
        private Bank(Item[] items) : base(DefaultBankSize)
        {
            _items = items;
        }

        public Bank() : base(DefaultBankSize)
        {
            
        }

        public string GetPageData(int page)
        {
            // check for out of range input.
            if (page > Pages ||
                page < 0) return null;

            int startIndex = page*ItemsPerPage;
            return ItemInfo(startIndex, startIndex + ItemsPerPage);
        }
    }
}