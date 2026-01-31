using UnityEngine;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Types;
using DungeonCrawler.Gameplay.Combat;
using DungeonCrawler.Core.Events; // optional: used if EventBus exists
using DungeonCrawler.Systems.CombatSystem;
using System.Collections;
using DungeonCrawler.Systems.Movement;
using UnityEngine.AI;
using UnityEngine.Events;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    [RequireComponent(typeof(Entity))]
    public class MeleeAttackHandler : MonoBehaviour, IAttackHandler
    {
        Entity _owner;
        EnemyType _archetype;
        float _cooldownTimer = 0f;

        // local refs for movement/agent control & animation
        NavMeshAgent _agent;
        IMovementController _movementController;
        Animator _animator;

        // timing defaults (can be tweaked on the component)
        [Header("Melee timing")]
        public float AttackWindUp = 0.25f;
        public float AttackRelease = 0.5f;
        public float RotationSpeed = 10f;

        [Header("External logic trigger")]

        public UnityEvent unityEvent = new UnityEvent();

        // internal state
        bool _isAttacking = false;

        public void Initialize(Entity owner, EnemyType archetype)
        {
            _owner = owner;
            _archetype = archetype;
            _cooldownTimer = 0f;

            _agent = owner.GetComponent<NavMeshAgent>();
            _movementController = owner.GetComponent<IMovementController>();
            _animator = owner.GetComponentInChildren<Animator>();

            // if archetype is present and you want to override timings from archetype, implement here.
            // (Archetype may not provide windup/release — keep component fields for quick tuning.)
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Entity target)
        {
            if (_archetype == null || target == null || !target.gameObject.activeInHierarchy) return false;
            if (_cooldownTimer > 0f) return false;
            if (_isAttacking) return false;

            // start attack sequence (non-blocking)
            StartCoroutine(DoAttackCoroutine(target));

            // Set internal cooldown (handler-side). The AI also sets its own _attackTimer.
            _cooldownTimer = _archetype.AttackCooldown;
            return true;
        }

        IEnumerator DoAttackCoroutine(Entity target)
        {
            _isAttacking = true;

            // stop movement
            if (_movementController != null) _movementController.Stop();
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;

            // set animator attack flag
            if (_animator != null) _animator.SetBool("isAttack", true);
            unityEvent?.Invoke();

            // keep facing the target during windup (one look and small smoothing)
            if (target != null)
            {
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Lerp(transform.rotation, look, Time.deltaTime * RotationSpeed);
                }
            }

            // wind-up
            yield return new WaitForSeconds(AttackWindUp);

            // check if target is still valid and in range; if so, deal damage
            bool didDamage = false;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                var health = target.GetComponent<Health>();
                // compute distance using archetype attack range if available
                float effectiveRange = _archetype.AttackRange;
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (health != null && health.GetCurrentHp() > 0 && dist <= effectiveRange + 0.2f)
                {
                    if (EventBus.Instance != null)
                    {
                        var dmg = new DamageEvent(target, _owner, _archetype.AttackDamage);
                        EventBus.Instance.Enqueue(dmg);
                        didDamage = true;
                        //Debug.Log($"{name} dealt {_archetype.AttackDamage} to {target.name}");
                    }
                }
            }

            // release portion — animation continues
            yield return new WaitForSeconds(AttackRelease);

            // clear animator and re-enable movement
            if (_animator != null) _animator.SetBool("isAttack", false);

            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = false;
            // movement controller remains stopped; EnemyAI Update will re-issue MoveTo next tick if appropriate.

            _isAttacking = false;
        }
    }
}
