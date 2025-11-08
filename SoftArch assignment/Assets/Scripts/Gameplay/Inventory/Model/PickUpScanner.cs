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

        void Update()
        {
            // Optionally do this only on input or every N frames
            Collider[] hits = Physics.OverlapSphere(transform.position, PickupRadius, PickupLayer);
            int taken = 0;
            foreach (var c in hits)
            {
                if (taken >= MaxPerScan) break;
                var pickup = c.GetComponent<PickupItem>();
                if (pickup == null) continue;

                // Attempt to pick up
                var inv = GetComponent<Inventory>();
                if (inv == null) continue;
                int added = inv.Add(pickup.Item, pickup.Quantity);
                if (added == pickup.Quantity)
                {
                    if (EventBus.Instance != null)
                        EventBus.Instance.Enqueue(new ItemPickedEvent(GetComponent<Entity>(), pickup.GetComponent<Entity>(), pickup.Item, added));
                    Destroy(pickup.gameObject);
                }
                else if (added > 0)
                {
                    if (EventBus.Instance != null)
                        EventBus.Instance.Enqueue(new ItemPickedEvent(GetComponent<Entity>(), pickup.GetComponent<Entity>(), pickup.Item, added));
                    pickup.Quantity -= added;
                }
                taken++;
            }
        }
    }
}