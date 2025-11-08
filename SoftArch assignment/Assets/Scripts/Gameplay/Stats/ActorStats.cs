using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats
{
    /// <summary>
    /// Universal per-entity stats, including leveling data.
    /// Attach this to any Entity that should level/gain XP (player, pets, etc).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Entity))]
    public class ActorStats : MonoBehaviour
    {
        [Tooltip("Optional per-actor LevelConfig. If null, LevelSystem.DefaultConfig will be used.")]
        public LevelConfig LevelConfig;

        [Header("Progression")]
        [Tooltip("Current level (1-based)")]
        public int Level = 1;

        [Tooltip("Do I really need to explain?")]
        public int currentXp = 0;

        [Header("Base Stats (example)")]
        public int Strength = 1;
        public int Vitality = 1;
        public int Intelligence = 1;

        // Helper: get the Entity component on same GameObject
        public Entity Entity => GetComponent<Entity>();

        /// <summary>
        /// Adds experience to this actor and returns a list of LevelUpEvent objects
        /// (one per level gained). You should pass a fallbackConfig when the actor's
        /// LevelConfig is null (the LevelSystem will provide DefaultConfig).
        /// </summary>
        public List<LevelUpEvent> AddExperience(int amount, LevelConfig fallbackConfig = null)
        {
            var evs = new List<LevelUpEvent>();
            if (amount <= 0) return evs;

            var cfg = LevelConfig != null ? LevelConfig : fallbackConfig;
            Debug.Log($"Experience received by {Entity.name} in amount: {amount}. I'll use config {cfg.name}");
            if (cfg == null)
            {
                Debug.LogWarning($"{name}: No LevelConfig available when adding XP. XP ignored.");
                return evs;
            }

            int remainingXp = currentXp + amount;

            while (true)
            {
                int xpNeeded = cfg.GetXpForLevel(Level);
                if (xpNeeded <= 0)
                {
                    // protect against bad config (avoid infinite loop)
                    Debug.LogError($"{name}: LevelConfig.GetXpForLevel returned <= 0 for level {Level}. Aborting progression.");
                    break;
                }

                if (remainingXp >= xpNeeded)
                {
                    remainingXp -= xpNeeded;
                    int oldLevel = Level;
                    Level++;
                    // create LevelUpEvent for listeners (LevelSystem or others can enqueue)
                    var entityRef = Entity;
                    var lvlEv = new LevelUpEvent(entityRef, oldLevel, Level, remainingXp);
                    evs.Add(lvlEv);
                }
                else break;

                // safety cap
                if (Level > 10000) break;
            }

            currentXp = remainingXp;
            return evs;
        }

        /// <summary>
        /// Convenience: returns XP required for next level using configured or fallback config.
        /// </summary>
        public int GetXpForNextLevel(LevelConfig fallbackConfig = null)
        {
            var cfg = LevelConfig != null ? LevelConfig : fallbackConfig;
            if (cfg == null) return int.MaxValue;
            return cfg.GetXpForLevel(Level);
        }
    }
}
