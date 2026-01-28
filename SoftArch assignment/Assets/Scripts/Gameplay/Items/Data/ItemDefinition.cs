using DungeonCrawler.Core.Utils;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items.Data
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Items/ItemDefinition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Unique id string for designer reference (not required for runtime)")]
        public string ItemId;

        public string ItemName;
        [TextArea(2, 6)]
        public string Description;
        public Sprite Icon;

        [Tooltip("Max items per stack. 1 = non-stackable")]
        public int MaxStack = 1;

        [Tooltip("If true this item will be consumed when used")]
        public bool IsConsumable = true;
        [Tooltip("How many consumptions are availible per item")]
        public int ConsumptionsAmount = 1;

        [Tooltip("World prefab to spawn when dropping or spawning in world")]
        public GameObject WorldPrefab;



        // Additional fields: weight, rarity, tags, type, stats modifiers, etc. IF needed.

        /// <summary>
        /// Returns whether the given user (GameObject) is allowed to use this item right now.
        /// Called before Use. Override to add checks (cooldowns, mana, etc).
        /// TODO: call this from server-side logic (authoritative). (probably use the player joint for this)
        /// </summary>
        public virtual bool CanUse(GameObject user)
        {
            return true;
        }

        /// <summary>
        /// Called to apply the item effect. Return true if the use succeeded (so caller can remove/consume).
        /// This method should be executed on the server (authoritative). Override in derived
        /// ScriptableObjects to implement concrete behavior (heal, buff, spawn, etc).
        /// Parameters:
        ///   user - the GameObject who is using the item (usually the player / entity root)
        ///   slotIndex - the inventory slot index where this item came from (optional for now)
        /// </summary>
        public virtual bool Use(GameObject user, int slotIndex)
        {
            return false;
        }
    }
}
