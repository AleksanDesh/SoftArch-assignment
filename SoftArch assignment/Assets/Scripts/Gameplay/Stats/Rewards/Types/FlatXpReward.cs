using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Stats.Rewards
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Rewards/FlatXP")]
    public class FlatXpReward : XpRewardCalculator
    {
        public int Amount = 50;

        public override int CalculateReward(Entity dead, ActorStats deadStats, Entity killer, ActorStats killerStats, LevelConfig fallbackConfig)
        {
            return Mathf.Max(0, Amount);
        }
    }
}
