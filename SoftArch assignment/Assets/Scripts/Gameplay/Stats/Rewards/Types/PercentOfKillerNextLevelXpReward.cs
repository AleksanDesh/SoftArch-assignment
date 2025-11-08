using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Rewards/PercentOfKillerNextLevelXP")]
    public class PercentOfKillerNextLevelXpReward : XpRewardCalculator
    {
        [Range(0f, 1f)]
        public float Percentage = 0.5f; // half of XP needed for killer to reach next level

        public override int CalculateReward(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig)
        {
            if (killerStats == null) return 0;
            var cfg = killerStats.LevelConfig != null ? killerStats.LevelConfig : fallbackConfig;
            if (cfg == null) return 0;

            int needed = cfg.GetXpForLevel(killerStats.Level);
            int val = Mathf.FloorToInt(needed * Percentage);
            return Mathf.Max(0, val);
        }
    }
}
