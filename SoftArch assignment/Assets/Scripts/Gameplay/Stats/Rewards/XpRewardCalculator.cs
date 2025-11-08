using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    /// <summary>
    /// Base SO for XP reward calculators. Implement CalculateReward to return an int XP amount.
    /// </summary>
    public abstract class XpRewardCalculator : ScriptableObject
    {
        /// <summary>
        /// Calculate how much XP the killer should receive when 'dead' dies.
        /// 'dead' and 'killer' may be null (defensive); fallbackConfig is provided so calculator
        /// can consult a LevelConfig if needed.
        /// </summary>
        public abstract int CalculateReward(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig);
    }
}
