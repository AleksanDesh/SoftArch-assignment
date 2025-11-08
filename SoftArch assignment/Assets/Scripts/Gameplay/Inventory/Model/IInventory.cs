using System.Collections.Generic;
using DungeonCrawler.Gameplay.Items.Data;

namespace DungeonCrawler.Gameplay.Inventory.Model
{
    public interface IInventory
    {
        int Capacity { get; }

        IReadOnlyList<InventorySlot> Slots { get; }

        /// <summary>Try to add a quantity. Returns how many were actually added.</summary>
        int Add(ItemDefinition def, int quantity = 1);

        /// <summary>Remove up to quantity from slot index. Returns how many removed.</summary>
        int RemoveAt(int slotIndex, int quantity = 1);

        /// <summary>Use item in given slot. Returns true if used.</summary>
        bool Use(int slotIndex);

        /// <summary>Find first slot index containing the given item, or -1 if not found.</summary>
        int Find(ItemDefinition def);
    }
}
