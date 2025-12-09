using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Inventory.Model
{
    public class PickupScanner : MonoBehaviour
    {
        [Tooltip("Sphere radius in which we check for pickups")]
        [SerializeField] private float PickupRadius = 1.5f;
        [Tooltip("Layers to check, less = better")]
        [SerializeField] private LayerMask PickupLayer;
        [Tooltip("How much unique items we can pick up per iteration")]
        [SerializeField] private int MaxPerScan = 5;

        Inventory _inventory;
        Entity _entity;
        private void Start()
        {
            _inventory = GetComponent<Inventory>();
            _entity = GetComponent<Entity>();
        }
        void Update()
        {
            // Optionally do this only on input or every N frames
            Collider[] hits = Physics.OverlapSphere(transform.position, PickupRadius, PickupLayer);
            int taken = 0;
            foreach (var c in hits)
            {
                if (taken >= MaxPerScan) break;
                if (_inventory == null) continue;
                if (!c.TryGetComponent<PickupItem>(out var pickup)) continue;

                int wanted = pickup.Quantity;
                if (wanted <= 0) continue;


                int added = _inventory.Add(pickup.Item, wanted);
                if (added <= 0) continue;

                int removed = pickup.TryTake(added);
                if (EventBus.Instance != null)
                    EventBus.Instance.Enqueue(new ItemPickedEvent(_entity, pickup.GetComponent<Entity>(), pickup.Item, removed));
                taken++;
            }
        }
    }
}