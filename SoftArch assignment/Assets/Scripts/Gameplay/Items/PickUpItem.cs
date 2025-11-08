using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        public ItemDefinition Item;
        public int Quantity = 1;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }


        // Currently not in use, pickup is managed through the player.

        //void OnTriggerEnter(Collider other)
        //{
        //    var ent = other.GetComponent<Entity>();
        //    if (ent == null) return;

        //    var inv = other.GetComponent<Inventory.Model.Inventory>();
        //    if (inv == null) return; // or use InventoryManager to find

        //    int added = inv.Add(Item, Quantity);
        //    if (added == Quantity)
        //    {
        //        if (EventBus.Instance != null)
        //        {
        //            EventBus.Instance.Enqueue(new ItemPickedEvent(ent, GetComponent<Entity>(), Item, added));
        //        }
        //        // optionally play pickup SFX, VFX
        //        Destroy(gameObject);
        //    }
        //    else if (added > 0)
        //    {
        //        if (EventBus.Instance != null)
        //        {
        //            EventBus.Instance.Enqueue(new ItemPickedEvent(ent, GetComponent<Entity>(), Item, added));
        //        }
        //        Debug.Log($"Picked up {added} items, out of {Quantity}");
        //        Quantity -= added;
        //    }
        //    else
        //    {
        //        Debug.Log($"Failed picking up item '{Item.ItemName}'");
        //    }
        //}
    }
}
