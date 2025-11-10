using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Inventory.Model
{
    public class PickupScanner : MonoBehaviour
    {
        [Tooltip("Sphere radius in which we check for pickups")]
        public float PickupRadius = 1.5f;
        [Tooltip("Layers to check, less = better")]
        public LayerMask PickupLayer;
        [Tooltip("How much unique items we can pick up per iteration")]
        public int MaxPerScan = 5;

        Inventory inventory;
        Entity entity;
        private void Start()
        {
            inventory = GetComponent<Inventory>();
            entity = GetComponent<Entity>();
        }
        void Update()
        {
            // Optionally do this only on input or every N frames
            Collider[] hits = Physics.OverlapSphere(transform.position, PickupRadius, PickupLayer);
            int taken = 0;
            foreach (var c in hits)
            {
                if (taken >= MaxPerScan) break;
                if (inventory == null) continue;
                if (!c.TryGetComponent<PickupItem>(out var pickup)) continue;

                int wanted = pickup.Quantity;
                if (wanted <= 0) continue;


                int added = inventory.Add(pickup.Item, wanted);
                if (added <= 0) continue;

                int removed = pickup.TryTake(added);
                if (EventBus.Instance != null)
                    EventBus.Instance.Enqueue(new ItemPickedEvent(entity, pickup.GetComponent<Entity>(), pickup.Item, removed));
                taken++;
            }
        }
    }
}