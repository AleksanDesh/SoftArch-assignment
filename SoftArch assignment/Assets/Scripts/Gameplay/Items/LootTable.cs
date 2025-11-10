using DungeonCrawler.Gameplay.Items.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items
{
    [Serializable]
    public class LootEntry
    {
        public ItemDefinition Item;
        [Range(0f, 1f)] public float Chance = 1f;
        public int MinQuantity = 1;
        public int MaxQuantity = 1;
    }

    [CreateAssetMenu(fileName = "DungeonCrawler/LootTable", menuName = "DungeonCrawler/NewLootTable")]
    public class LootTable : ScriptableObject
    {
        public List<LootEntry> Entries = new List<LootEntry>();
        [Tooltip("How many independent rolls to perform when rolling this table.")]
        public int Rolls = 1;

        // simple roll: returns list of (item, qty)
        public List<(ItemDefinition item, int qty)> Roll(System.Random rng = null)
        {
            if (rng == null) rng = new System.Random();
            var results = new List<(ItemDefinition, int)>();
            for (int r = 0; r < Math.Max(1, Rolls); r++)
            {
                foreach (var e in Entries)
                {
                    if (e.Item == null) continue;
                    if (e.Chance < 1f)
                    {
                        if (rng.NextDouble() > e.Chance) continue;
                    }
                    int qty = UnityEngine.Random.Range(e.MinQuantity, e.MaxQuantity + 1);
                    if (qty <= 0) continue;
                    results.Add((e.Item, qty));
                }
            }
            return results;
        }
    }
}