using System;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items.Data;
using DungeonCrawler.Systems.Gameflow;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Inventory.Model
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Entity))]
    public class Inventory : MonoBehaviour, IInventory
    {
        [Tooltip("Number of slots in this inventory")]
        [SerializeField] int capacity = 20;
        public int Capacity => capacity;

        [SerializeField]
        List<InventorySlot> slots = new List<InventorySlot>();

        // Expose readonly list
        public IReadOnlyList<InventorySlot> Slots => slots.AsReadOnly();


        // Parameter is slot index that changed.
        public Action<int> OnSlotChanged;

        /// <summary>
        /// Invoked when a slot item is 'used' (slot index).
        /// For consumables this will normally be fired indirectly via OnSlotChanged when RemoveAt is called.
        /// For non-consumables this fires when Use() handles the action.
        /// </summary>
        public Action<int> OnSlotUsed;

        void Awake()
        {
            // ensure slots list length
            while (slots.Count < capacity) slots.Add(new InventorySlot(null, 0));
            if (slots.Count > capacity) slots.RemoveRange(capacity, slots.Count - capacity);
        }

        void OnEnable()
        {
            var ent = GetComponent<Entity>();
            if (ent != null && InventoryManager.Instance != null)
                InventoryManager.Instance.Register(ent, this);
        }

        void OnDisable()
        {
            var ent = GetComponent<Entity>();
            if (ent != null && InventoryManager.Instance != null)
                InventoryManager.Instance.Unregister(ent);
        }

        /// <summary>Try to add quantity of item. Returns how many were actually added.</summary>
        public int Add(ItemDefinition def, int quantity = 1)
        {
            if (def == null || quantity <= 0) return 0;

            int toAdd = quantity;

            // First try to fill existing stacks
            if (def.MaxStack > 1)
            {
                for (int i = 0; i < capacity && toAdd > 0; i++)
                {
                    var s = slots[i];
                    if (s.Definition == def && s.Quantity < def.MaxStack)
                    {
                        int space = def.MaxStack - s.Quantity;
                        int addNow = Mathf.Min(space, toAdd);
                        s.Quantity += addNow;
                        toAdd -= addNow;
                        slots[i] = s;
                        OnSlotChanged?.Invoke(i);
                    }
                }
            }

            // Then try empty slots
            for (int i = 0; i < capacity && toAdd > 0; i++)
            {
                var s = slots[i];
                if (s.IsEmpty)
                {
                    int put = Mathf.Min(def.MaxStack, toAdd);
                    slots[i] = new InventorySlot(def, put);
                    toAdd -= put;
                    OnSlotChanged?.Invoke(i);
                }
            }

            return quantity - toAdd; // added
        }

        /// <summary>Remove up to quantity from slot index. Returns how many removed.</summary>
        public int RemoveAt(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= capacity) return 0;
            var s = slots[slotIndex];
            if (s.IsEmpty) return 0;

            int removed = Mathf.Min(quantity, s.Quantity);
            s.Quantity -= removed;
            if (s.Quantity <= 0) slots[slotIndex] = new InventorySlot(null, 0);
            else slots[slotIndex] = s;

            OnSlotChanged?.Invoke(slotIndex);
            return removed;
        }

        // Remove multiple items, works across stacks
        public int RemoveItems(ItemDefinition def, int quantity = 1)
        {
            if (def == null || quantity <= 0) return 0;
            int remaining = quantity;

            // remove from stacks preferring last slots first (so UI compaction is predictable)
            for (int i = capacity - 1; i >= 0 && remaining > 0; i--)
            {
                var s = slots[i];
                if (s.IsEmpty || s.Definition != def) continue;
                int removed = RemoveAt(i, remaining);
                remaining -= removed;
            }

            int actuallyRemoved = quantity - remaining;
            return actuallyRemoved;
        }

        /// <summary>Use item in given slot. Implement simple consumption; return true if used.</summary>
        public bool Use(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= capacity) return false;
            var s = slots[slotIndex];
            if (s.IsEmpty) return false;

            // Example: if consumable, reduce and call OnUse hooks
            if (s.Definition.IsConsumable && s.Definition.CanUse(this.gameObject))
            {
                
                bool used = s.Definition.Use(this.gameObject, slotIndex);
                if (used)
                {
                    s.Uses++;
                    if (s.Uses >= s.Definition.ConsumptionsAmount)
                        RemoveAt(slotIndex, 1); //  TODO: server-side removal 

                    OnSlotChanged?.Invoke(slotIndex);
                    OnSlotUsed?.Invoke(slotIndex);
                    return true;
                }
                return false;
            }
            else
            {
                // Currently nothing, but:
                // Non-consumable: equip or open UI
                // notify listeners that a use action occurred (UI might open details / equip, etc.)
                // OnSlotChanged?.Invoke(slotIndex);
                // OnSlotUsed?.Invoke(slotIndex);
                return false;
            }
        }

        public int Find(ItemDefinition def)
        {
            if (def == null) return -1;
            for (int i = 0; i < capacity; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty && s.Definition == def) return i;
            }
            return -1;
        }
    }
}
