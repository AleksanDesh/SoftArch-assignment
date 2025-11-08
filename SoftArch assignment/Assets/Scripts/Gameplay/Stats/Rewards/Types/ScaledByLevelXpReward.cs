using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Rewards/ScaledByLevel")]
    public class ScaledByLevelXpReward : XpRewardCalculator
    {
        public int BaseAmount = 20;
        public float PerLevelMultiplier = 1.25f; // Amount = Base * level * multiplier

        public override int CalculateReward(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig)
        {
            int lvl = deadStats != null ? Mathf.Max(1, deadStats.Level) : 1;
            float val = BaseAmount * lvl * PerLevelMultiplier;
            return Mathf.Max(0, Mathf.FloorToInt(val));
        }
    }
}
