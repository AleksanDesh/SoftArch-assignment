using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Combat;
using DungeonCrawler.Gameplay.Enemy.Types;
using DungeonCrawler.Systems.Movement;
using DungeonCrawler.Systems.CombatSystem;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Targeting / Aggro")]
        [Tooltip("Which entity tag to consider 'player' (alternatively register player with EntityManager and set PlayerTag to empty).")]
        public string PlayerTag = "Player";
        public float AggroRange = 10f;
        [Tooltip("How long (seconds) the enemy remains aggressive after losing sight / leaving aggro range.")]
        public float AggroDuration = 3f;

        [Header("Combat (defaults; can be overridden by Archetype)")]
        public float AttackRange = 2f;
        public float AttackCooldown = 1.2f;
        public int AttackDamage = 10;

        [Header("Movement (defaults; can be overridden by Archetype)")]
        public float StoppingDistance = 1f;

        [Header("Optional Archetype")]
        [Tooltip("Optional ScriptableObject to provide stats. If assigned, archetype values override the fields above on Start.")]
        public MeleeEnemyType Archetype;

        [Header("Optional modular components (auto-detected if left empty)")]
        [Tooltip("Assign the movement controller component that implements IMovementController, or leave null to auto-detect.")]
        public MonoBehaviour MovementControllerComponent;
        [Tooltip("Assign the attack handler component that implements IAttackHandler, or leave null to auto-detect.")]
        public MonoBehaviour AttackHandlerComponent;

        // internal cached refs
        Entity _entity;
        NavMeshAgent _agent;

        // modular interfaces (may be null; fallback to navmesh agent + direct damage remains)
        IMovementController _movementController;
        IAttackHandler _attackHandler;

        // target management
        Entity _target;
        float _aggroTimer = 0f;
        float _attackTimer = 0f;
        bool stunned = false;

        void Start()
        {
            _entity = GetComponent<Entity>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = StoppingDistance;

            // Apply archetype overrides if present
            if (Archetype != null)
            {
                // apply movement/combat overrides from archetype (keeps your public fields in inspector but uses archetype values)
                AttackRange = Archetype.AttackRange;
                AttackCooldown = Archetype.AttackCooldown;
                AttackDamage = Archetype.AttackDamage;
                AggroRange = Archetype.AggroRange;
                AggroDuration = Archetype.AggroDuration;
                StoppingDistance = Archetype.StoppingDistance;

                // update agent if present
                _agent.speed = Archetype.MoveSpeed;
                _agent.acceleration = Archetype.Acceleration;
                _agent.stoppingDistance = StoppingDistance;
            }

            // Ensure agent is on NavMesh (attempt to snap/warp if not)
            if (!_agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                    Debug.Log($"{name}: Warped to NavMesh at start.");
                }
                else
                {
                    Debug.LogWarning($"{name}: not on NavMesh and no sample found within 2m. Enemy may not move.");
                }
            }

            // Resolve modular components (prefer inspector-assigned components; else auto-detect)
            if (MovementControllerComponent != null && MovementControllerComponent is IMovementController mc)
            {
                _movementController = mc;
                _movementController.Initialize(_entity, Archetype);
            }
            else
            {
                _movementController = GetComponent<IMovementController>();
                _movementController?.Initialize(_entity, Archetype);
            }

            if (AttackHandlerComponent != null && AttackHandlerComponent is IAttackHandler ah)
            {
                _attackHandler = ah;
                _attack_handler_initialize_safe(ah);
            }
            else
            {
                _attack_handler_initialize_safe(GetComponent<IAttackHandler>()); // may be null
            }

            // try to find initial player via EntityManager; fallback to GameObject.FindWithTag later
            // Idk why i'm making my life harder.
            if (EntityManager.Instance != null)
            {
                _target = EntityManager.Instance.GetClosest(transform.position, PlayerTag);
            }
            if (_target == null && !string.IsNullOrEmpty(PlayerTag))
            {
                var go = GameObject.FindWithTag(PlayerTag);
                if (go != null) _target = go.GetComponent<Entity>();
            }
        }

        // helper to safely initialize attack handler
        void _attack_handler_initialize_safe(IAttackHandler handler)
        {
            _attackHandler = handler;
            if (_attackHandler != null)
            {
                _attackHandler.Initialize(_entity, Archetype);
            }
        }

        void Update()
        {
            // update timers
            float dt = Time.deltaTime;
            if (_aggroTimer > 0f) _aggroTimer -= dt;
            if (_attackTimer > 0f) _attackTimer -= dt;

            // Ensure we have a target reference; if we don't, try to acquire one
            if (_target == null)
            {
                if (EntityManager.Instance != null)
                    _target = EntityManager.Instance.GetClosest(transform.position, PlayerTag);

                if (_target == null && !string.IsNullOrEmpty(PlayerTag))
                {
                    var go = GameObject.FindWithTag(PlayerTag);
                    if (go != null) _target = go.GetComponent<Entity>();
                }
            }

            if (_target == null) return;

            float distSqr = (_target.transform.position - transform.position).sqrMagnitude;
            bool inAggroRange = distSqr <= AggroRange * AggroRange;

            // refresh or set aggro timer if player in range
            if (inAggroRange)
            {
                _aggroTimer = AggroDuration;
            }

            // if currently aggressive (timer > 0), chase
            if (_aggroTimer > 0f)
            {
                // Movement: prefer modular movement controller
                if (_movementController != null)
                {
                    // if not stunned, move; otherwise stop movement controller
                    if (!stunned)
                        _movementController.MoveTo(_target.transform.position);
                    else
                        _movementController.Stop();
                }
                else
                {
                    // fallback: direct NavMeshAgent behavior (keeps previous behavior)
                    if (_agent.isOnNavMesh)
                    {
                        if (!_agent.enabled) _agent.enabled = true;      // sanity
                        if (!stunned && _agent.isStopped) _agent.isStopped = false;  // UNSTOP the agent

                        _agent.SetDestination(_target.transform.position);
                    }
                    else
                    {
                        Debug.Log("Agent is not on a NavMesh");
                    }
                }

                // Attack if in melee range (uses modular attack handler when available)
                if (distSqr <= AttackRange * AttackRange && _attackTimer <= 0f)
                {
                    bool attacked = false;
                    if (_attackHandler != null)
                    {
                        attacked = _attackHandler.TryAttack(_target);
                    }
                    else
                    {
                        attacked = TryAttackTarget_Fallback();
                    }

                    if (attacked)
                    {
                        _attackTimer = AttackCooldown;
                    }
                }
            }
            else
            {
                // Not aggroed: stop moving
                if (_movementController != null)
                {
                    _movementController.Stop();
                }
                else if (_agent.isOnNavMesh && !_agent.isStopped)
                {
                    _agent.ResetPath();
                    _agent.isStopped = true;
                }
            }
        }

        // fallback direct attack logic
        bool TryAttackTarget_Fallback()
        {
            if (_target == null) return false;
            Debug.Log("Fallback attack");

            var health = _target.GetComponent<Health>();
            if (health != null)
            {
                health.ApplyDamage(AttackDamage, _entity);
                return true;
            }
            else
            {
                Debug.Log($"{name} attacked {_target.name} for {AttackDamage}, but target has no Health component.");
                return false;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
