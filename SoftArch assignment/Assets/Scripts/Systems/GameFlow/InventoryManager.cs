using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Inventory.Model;

namespace DungeonCrawler.Systems.Gameflow
{
    [DefaultExecutionOrder(-900)]
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        readonly Dictionary<int, Inventory> _byEntity = new Dictionary<int, Inventory>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(Entity e, Inventory inv)
        {
            if (e == null || inv == null) return;
            _byEntity[e.Id] = inv;
        }

        public void Unregister(Entity e)
        {
            if (e == null) return;
            _byEntity.Remove(e.Id);
        }

        public Inventory GetInventoryByEntityId(int id)
        {
            _byEntity.TryGetValue(id, out var inv);
            return inv;
        }
    }
}
