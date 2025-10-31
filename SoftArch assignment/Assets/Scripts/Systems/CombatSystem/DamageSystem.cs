using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Combat;
using UnityEngine;

namespace DungeonCrawler.Systems.CombatSystem
{
    // A centralized system that listens for TrapTriggeredEvent and DamageEvent
    public class DamageSystem : MonoBehaviour
    {
        void Start()
        {
            EventBus.Instance.Subscribe<TrapTriggeredEvent>(OnTrapTriggered);
            EventBus.Instance.Subscribe<DamageEvent>(OnDamageEvent);
        }

        void OnDestroy()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<TrapTriggeredEvent>(OnTrapTriggered);
                EventBus.Instance.Unsubscribe<DamageEvent>(OnDamageEvent);
            }
        }

        // Consider removing this, and working through damage event + passing debuff data
        void OnTrapTriggered(TrapTriggeredEvent ev)
        {
            if (ev.Consumed) return;
            // Convert trap trigger into a DamageEvent (decoupling source)
            var dmg = new DamageEvent(ev.TargetEntity, ev.SourceEntity, ev.Damage);
            EventBus.Instance.Enqueue(dmg);
            // ev.Consumed = true;
        }

        void OnDamageEvent(DamageEvent ev)
        {
            if (ev.Consumed) return;
            var target = EntityManager.Instance.GetById(ev.TargetEntity.Id);
            if (target == null) return;
            var health = target.GetComponent<Health>();
            if (health == null) return;

            health.ApplyDamage(ev.Amount, ev.SourceEntity);

            // Optionally consume event
            ev.Consumed = true;
        }
    }
}