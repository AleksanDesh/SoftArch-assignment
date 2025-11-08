using System;
using DungeonCrawler.Gameplay.Items.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Inventory.Model
{
    [Serializable]
    public struct InventorySlot
    {
        public ItemDefinition Definition;
        public int Quantity;

        public bool IsEmpty => Definition == null || Quantity <= 0;

        public InventorySlot(ItemDefinition def, int qty)
        {
            Definition = def;
            Quantity = qty;
        }
    }
}
