// DungeonCrawler.Gameplay.Rewards
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items
{
    [DisallowMultipleComponent]
    public class LootDropper : MonoBehaviour
    {
        [Tooltip("ScriptableObject loot table")]
        public LootTable Table;

        [Tooltip("If true, attempt to directly give items to killer's inventory before spawning world pickups.")]
        public bool AutoGrantToKiller = true;

        [Tooltip("Prefab fallback used when ItemDefinition.WorldPrefab is null. Sphere by default")]
        public GameObject DefaultPickupPrefab;

        [Tooltip("Max radius to scatter spawned pickups on XZ plane")]
        public float JitterRadius = 0.6f;

        void Awake()
        {
            if (DefaultPickupPrefab == null)
                DefaultPickupPrefab = Resources.Load<GameObject>("Prefabs/Defaults/PickupPrf");
        }

        /// <summary>Rolls the loot and spawns/gives results. 'killer' can be null (e.g. environment death).</summary>
        public void HandleLoot(Entity dead, Entity killer, Vector3 worldPos)
        {
            if (Table == null) return;
            var results = Table.Roll();

            foreach (var (itemDef, qty) in results)
            {
                if (itemDef == null || qty <= 0) continue;

                // try to give to killer's inventory first (if allowed)
                int remaining = qty;
                if (AutoGrantToKiller && killer != null)
                {
                    var inv = killer.GetComponentInChildren<Inventory.Model.Inventory>();
                    if (inv != null)
                    {
                        int added = inv.Add(itemDef, qty);
                        remaining = qty - added;
                    }
                }

                // spawn pickup(s) for remaining
                if (remaining > 0)
                {
                    // spawn possibly as multiple stacks according to item.MaxStack
                    int maxStack = Mathf.Max(1, itemDef.MaxStack);
                    int toSpawn = Mathf.CeilToInt((float)remaining / maxStack);
                    int left = remaining;
                    for (int s = 0; s < toSpawn; s++)
                    {
                        int take = Mathf.Min(left, maxStack);
                        Vector2 rnd = Random.insideUnitCircle * JitterRadius;
                        Vector3 pos = worldPos + new Vector3(rnd.x, 0f, rnd.y);
                        SpawnPickupAt(itemDef, take, pos);
                        left -= take;
                    }
                }
            }
        }

        void SpawnPickupAt(ItemDefinition def, int qty, Vector3 pos)
        {
            // Prefer ItemDefinition.WorldPrefab if provided; else use DefaultPickupPrefab
            GameObject prefab = def.WorldPrefab != null ? def.WorldPrefab : DefaultPickupPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"No pickup prefab for item {def?.ItemName}");
                return;
            }
            //Debug.Log($"Spawning pickUpItem at position {pos}");
            PickupItem.Spawn(prefab, def, qty, pos, Quaternion.identity);
            
        }
    }
}
