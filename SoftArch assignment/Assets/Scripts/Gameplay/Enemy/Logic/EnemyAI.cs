using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Combat;
using DungeonCrawler.Gameplay.Enemy.Types;
using DungeonCrawler.Systems.CombatSystem;
using DungeonCrawler.Systems.Movement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting / Aggro")]
        //[Tooltip("Which entity tag to consider 'player' (alternatively register player with EntityManager and set PlayerTag to empty).")]
        //public string PlayerTag = "Player";
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
        public EnemyType Archetype;

        [Header("Optional modular components (auto-detected if left empty)")]
        [Tooltip("Assign the movement controller component that implements IMovementController, or leave null to auto-detect.")]
        public MonoBehaviour MovementControllerComponent;
        [Tooltip("Assign the attack handler component that implements IAttackHandler, or leave null to auto-detect.")]
        public MonoBehaviour AttackHandlerComponent;
        [Header("Animator (optional)")]
        [Tooltip("Assign an Animator (will fallback to GetComponentInChildren<Animator> if null).")]
        public Animator animator;
        [Header("Attack timing (visual/motion lock in seconds)")]
        public float AttackWindUp = 0.25f;
        public float AttackRelease = 0.5f;
        public float RotationSpeed = 10f;
        bool _isPerformingAttack = false;
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
        bool _stunned = false;


        #region Network


        void OnDestroy()
        {
            if (isServer)
                OnStopServer();
        }



        #endregion
        void Start()
        {
            _entity = GetComponent<Entity>();
            _agent = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _agent.stoppingDistance = StoppingDistance;

            // Apply archetype overrides if present
            if (Archetype != null)
            {
                AttackRange = Archetype.AttackRange;
                AttackCooldown = Archetype.AttackCooldown;
                AttackDamage = Archetype.AttackDamage;
                AggroRange = Archetype.AggroRange;
                AggroDuration = Archetype.AggroDuration;
                StoppingDistance = Archetype.StoppingDistance;

                _agent.speed = Archetype.MoveSpeed;
                _agent.acceleration = Archetype.Acceleration;
                _agent.stoppingDistance = StoppingDistance;
            }

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

            // Resolve modular components 
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
                AttackHanlderInitialize(ah);
            }
            else
            {
                AttackHanlderInitialize(GetComponent<IAttackHandler>()); // may be null
            }
        }

        // NOTE: minor rename fix vs earlier snippet (private field)
        void AttackHanlderInitialize(IAttackHandler handler)
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

            // Acquire or refresh the closest player target from the registry if needed
            if (_target == null || _target.gameObject.GetComponent<Health>().GetCurrentHp() <= 0)
            {
                _target = GetClosestPlayerFromRegistry();
            }

            if (_target == null)
            {
                // set idle animation
                SetAnimatorWalk(false);
                return;
            }

            float distSqr = (_target.transform.position - transform.position).sqrMagnitude;
            bool inAggroRange = distSqr <= AggroRange * AggroRange;

            if (inAggroRange)
            {
                _aggroTimer = AggroDuration;
            }

            if (_aggroTimer > 0f)
            {
                // Movement: only move if not stunned and not locked by an ongoing attack animation
                if (_movementController != null)
                {
                    if (!_stunned && !_isPerformingAttack)
                    {
                        _movementController.MoveTo(_target.transform.position);
                        SetAnimatorWalk(true);
                    }
                    else
                    {
                        _movementController.Stop();
                        SetAnimatorWalk(false);
                    }
                }
                else
                {
                    if (_agent.isOnNavMesh)
                    {
                        if (!_agent.enabled) _agent.enabled = true;

                        // stop the agent while performing attack
                        if (!_stunned && !_isPerformingAttack)
                        {
                            if (_agent.isStopped) _agent.isStopped = false;
                            _agent.SetDestination(_target.transform.position);
                            SetAnimatorWalk(true);
                        }
                        else
                        {
                            if (!_agent.isStopped) _agent.isStopped = true;
                            SetAnimatorWalk(false);
                        }
                    }
                    else
                    {
                        Debug.Log("Agent is not on a NavMesh");
                        SetAnimatorWalk(false);
                    }
                }

                // Attack decision
                if (distSqr <= AttackRange * AttackRange && _attackTimer <= 0f)
                {
                    // Before initiating attack — force stop movement so the attack animation/play is clean.
                    if (_movementController != null)
                        _movementController.Stop();
                    else if (_agent.isOnNavMesh)
                        _agent.isStopped = true;

                    // face the target immediately (so attack anim looks correct)
                    FaceTarget(_target.transform);

                    bool attacked = false;
                    if (_attackHandler != null)
                    {
                        attacked = _attackHandler.TryAttack(_target);
                    }

                    if (attacked)
                    {
                        _attackTimer = AttackCooldown;

                        // lock movement locally for the duration of the attack animation so Update doesn't re-enable movement
                        _isPerformingAttack = true;
                        StartCoroutine(ClearAttackLockAfter(AttackWindUp + AttackRelease));
                    }
                }
            }
            else
            {
                if (_movementController != null)
                {
                    _movementController.Stop();
                }
                else if (_agent.isOnNavMesh && !_agent.isStopped)
                {
                    _agent.ResetPath();
                    _agent.isStopped = true;
                }

                SetAnimatorWalk(false);
            }
        }

        IEnumerator ClearAttackLockAfter(float t)
        {
            yield return new WaitForSeconds(t);
            _isPerformingAttack = false;
        }

        void FaceTarget(Transform target)
        {
            if (target == null) return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * RotationSpeed);
        }

        void SetAnimatorWalk(bool v)
        {
            if (animator != null) animator.SetBool("isWalk", v);
        }

        void SetAnimatorAttack(bool v)
        {
            if (animator != null) animator.SetBool("isAttack", v);
        }



        // Returns the closest registered player Entity or null if none
        Entity GetClosestPlayerFromRegistry()
        {
            Entity best = null;
            float bestSqr = float.MaxValue;
            Vector3 pos = transform.position;
            float aggroSqr = AggroRange * AggroRange;

            foreach (var kv in NetworkServer.connections)
            {
                var conn = kv.Value;
                if (conn == null || conn.identity == null) continue;

                var go = conn.identity.gameObject;
                if (go == null) continue;

                // skip if not an Entity or player is dead
                var entity = go.GetComponent<Entity>();
                if (entity == null) continue;

                var health = go.GetComponent<Health>();
                if (health != null && health.GetCurrentHp() <= 0) continue;

                // distance check: only consider players within AggroRange
                float dSqr = (entity.transform.position - pos).sqrMagnitude;
                if (dSqr > aggroSqr) continue; // too far

                // choose the closest among those in range
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = entity;
                }
            }

            return best; // null if none within AggroRange
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }

        #region NetworkResolving
        public override void OnStartClient()
        {
            base.OnStartClient();

            // Disable on all clients except host
            if (!isServer)
            {
                enabled = false;
            }
        }
        #endregion
    }
}