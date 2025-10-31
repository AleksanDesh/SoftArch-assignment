using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Combat
{
    [RequireComponent(typeof(Entity))]
    public class Health : MonoBehaviour
    {
        public int MaxHP = 50;
        public int CurrentHP;

        Entity _entity;

        void Start()
        {
            _entity = GetComponent<Entity>();
            CurrentHP = MaxHP;
        }

        public void ApplyDamage(int amount, Entity damager)
        {
            if (amount <= 0) return;
            CurrentHP -= amount;
            Debug.Log($"{name} took {amount} damage from {damager.name}. HP: {CurrentHP}/{MaxHP}");

            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                // Enqueue DeathEvent
                Debug.Log("Death event called with current health = " + CurrentHP);
                var death = new DeathEvent(_entity, damager);
                Debug.Log("Health enqueued DeathEvent type = " + death.GetType().FullName);
                EventBus.Instance.Enqueue(death);
            }
        }
    }
}