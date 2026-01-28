using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using DungeonCrawler.Gameplay.Stats.Rewards;
using Mirror;
using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;


namespace DungeonCrawler.Gameplay.Combat
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(ActorStats))]
    [RequireComponent(typeof(XpRewardSource))]
    public class Health : NetworkBehaviour
    {

        [SerializeField] private int MaxHP = 50;
        public Action<Health> onHealthChanged;
        [SyncVar(hook = nameof(OnCurrentHpChanged))]
        private int CurrentHP;
        
        Entity _entity;

        //[Expandable]
        public bool godMode = false;

        void Awake()
        {
            _entity = GetComponent<Entity>();
            CurrentHP = MaxHP;
        }

        [ServerCallback]
        public void RestoreHp()
        {
            CurrentHP = MaxHP;
            onHealthChanged?.Invoke(this);
        }


        [ServerCallback]
        public void ApplyDamage(int amount, Entity damager, bool InstaKill = false)
        {
            if (amount <= 0 || godMode) return;
            CurrentHP -= amount;
            if (InstaKill) CurrentHP = 0;
            //InformHealthChange();
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
                //Debug.Log("Death event called with current health = " + CurrentHP);
                var death = new DeathEvent(_entity, damager, xpReward);
                //Debug.Log($"Health enqueued DeathEvent type {death.GetType().FullName}, and trying to add {xpReward} xp to the killer {damager.name}");
                EventBus.Instance.Enqueue(death);
            }
        }


        [ServerCallback]
        public void SetMaxHp(int value)
        {
            MaxHP = value;
            onHealthChanged?.Invoke(this);
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            onHealthChanged?.Invoke(this);
        }

        public int GetCurrentHp()
        {
            return CurrentHP;
        }

        public int GetMaxHP()
        {
            return MaxHP; 
        }

        void OnCurrentHpChanged(int oldValue, int newValue)
        {
            onHealthChanged?.Invoke(this);
        }
    }
}