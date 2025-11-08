using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Player.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Rewards/PercentOfDeadTotalXP")]
    public class PercentOfDeadTotalXpReward : XpRewardCalculator
    {
        [Range(0f, 1f)]
        public float Percentage = 0.2f; // 20% of dead's "total xp"

        public override int CalculateReward(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig)
        {
            if (deadStats == null) return 0;
            var cfg = deadStats.LevelConfig != null ? deadStats.LevelConfig : fallbackConfig;
            if (cfg == null) return 0;

            // Use cumulative XP up to dead's current level (or next level depending on desire).
            // Here we compute cumulative XP required to reach dead's current level (exclusive),
            // plus the XP needed to reach next level -> total invested XP baseline.
            int totalInvested = cfg.GetCumulativeXpForLevel(deadStats.Level + 1);
            int val = Mathf.FloorToInt(totalInvested * Percentage);
            return Mathf.Max(0, val);
        }
    }
}
