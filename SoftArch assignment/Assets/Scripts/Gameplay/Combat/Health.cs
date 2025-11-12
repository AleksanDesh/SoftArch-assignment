using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using DungeonCrawler.Gameplay.Stats.Rewards;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Combat
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(ActorStats))]
    [RequireComponent(typeof(XpRewardSource))]
    public class Health : MonoBehaviour
    {
        public int MaxHP = 50;
        public int CurrentHP;

        Entity _entity;

        public bool godMode = false;

        void Start()
        {
            _entity = GetComponent<Entity>();
            CurrentHP = MaxHP;
        }

        public void ApplyDamage(int amount, Entity damager)
        {
            if (amount <= 0 || godMode) return;
            CurrentHP -= amount;
            //Debug.Log($"{name} took {amount} damage from {damager.name}. HP: {CurrentHP}/{MaxHP}");

            if (CurrentHP <= 0)
            {
                CurrentHP = 0;

                int xpReward = 0;

                var stats = GetComponent<ActorStats>();
                var rewardSource = GetComponent<XpRewardSource>();
                if (rewardSource != null)
                {
                    xpReward = rewardSource.Calculate(_entity, stats, damager, damager?.GetComponent<ActorStats>(), null);
                }

                // Enqueue DeathEvent
                Debug.Log("Death event called with current health = " + CurrentHP);
                var death = new DeathEvent(_entity, damager, xpReward);
                Debug.Log($"Health enqueued DeathEvent type {death.GetType().FullName}, and trying to add {xpReward} xp to the killer {damager.name}");
                EventBus.Instance.Enqueue(death);
            }
        }
    }
}