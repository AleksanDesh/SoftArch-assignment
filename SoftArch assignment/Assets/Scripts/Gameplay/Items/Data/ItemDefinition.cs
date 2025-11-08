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

        [Tooltip("Optional world prefab to spawn when dropping or spawning in world")]
        public GameObject WorldPrefab;

        // Additional fields: weight, rarity, tags, type, stats modifiers, etc. IF needed.
    }
}
