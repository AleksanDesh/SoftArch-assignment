using DungeonCrawler.Gameplay.Combat;
using DungeonCrawler.Gameplay.Items.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items.Data
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Items/HealthPotion", fileName = "NewHealthPotion")]
    public class HealthPotionDefinition : ItemDefinition
    {
        [Tooltip("Amount of health restored when used")]
        public int HealAmount = 25;

        /// <summary>
        /// Example check: only allow use if the user has a Health component and is not full.
        /// This has to run server side (TODO)
        /// </summary>
        public override bool CanUse(GameObject user)
        {
            var health = user.GetComponent<Health>();
            if (health == null) return false;

            return health.GetCurrentHp() < health.GetMaxHP();
        }

        /// <summary>
        /// Apply the heal. This should be called on the server.
        /// Returns true on success (caller can consume the item).
        /// </summary>
        public override bool Use(GameObject user, int slotIndex)
        {
            var health = user.GetComponent<Health>();
            if (health == null) return false;
            health.Heal(HealAmount);

            // success: effect applied. Inventory/owner can remove one consumption/stack.
            return true;
        }
    }
}
