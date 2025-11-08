using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Player.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    /// <summary>
    /// Attach to enemy prefab (or put on enemy type SO). Holds reference to a ScriptableObject
    /// that calculates XP reward when this entity dies.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Entity))]
    public class XpRewardSource : MonoBehaviour
    {
        [Tooltip("If null, LevelSystem.DefaultConfig is used as fallback in calculators.")]
        public XpRewardCalculator Calculator;

        // Convenience: store override direct flat fallback
        public int FallbackFlatAmount = 0;

        public int Calculate(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig)
        {
            if (Calculator != null)
            {
                return Calculator.CalculateReward(dead, deadStats, killer, killerStats, fallbackConfig);
            }
            return Mathf.Max(0, FallbackFlatAmount);
        }
    }
}
