using UnityEngine;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Types;
using DungeonCrawler.Gameplay.Combat;
using DungeonCrawler.Core.Events; // optional: used if EventBus exists
using DungeonCrawler.Systems.CombatSystem;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    [RequireComponent(typeof(Entity))]
    public class MeleeAttackHandler : MonoBehaviour, IAttackHandler
    {
        Entity _owner;
        MeleeEnemyType _archetype;
        float _cooldownTimer = 0f;

        public void Initialize(Entity owner, MeleeEnemyType archetype)
        {
            _owner = owner;
            _archetype = archetype;
            _cooldownTimer = 0f;
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Entity target)
        {
            if (_archetype == null || target == null) return false;
            if (_cooldownTimer > 0f) return false;
            Debug.Log("Using MeleeAttackHandler");

            if (EventBus.Instance != null)
            {
                // DamageEvent(int targetEntityId, int amount) assumed from Core.Events
                var dmg = new DamageEvent(target, _owner, _archetype.AttackDamage);
                EventBus.Instance.Enqueue(dmg);
            }
            else
            {
                Debug.Log("I couldn't use the event bus");
                // Fallback: direct Health application
                var health = target.GetComponent<Health>();
                if (health != null)
                {
                    health.ApplyDamage(_archetype.AttackDamage, _owner);
                }
                else
                {
                    Debug.LogWarning($"{name}: Tried to attack {target.name} but it has no Health component and EventBus is unavailable.");
                }
            }

            _cooldownTimer = _archetype.AttackCooldown;
            // Optionally, play animations / VFX here
            return true;
        }
    }
}
