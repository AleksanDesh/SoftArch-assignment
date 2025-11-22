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
        public EnemyType Archetype;

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
        // ---------------------------
        // Static player registry API
        // ---------------------------
        // All player Entities should register/unregister themselves
        // This list is used by servers to target players; it's static so it scales across enemies.
        public static readonly List<Entity> PlayerEntities = new List<Entity>();

        #region Network
        static int s_serverEnemyCount = 0;           // how many EnemyAI instances are running on server
        static bool s_subscribedToNetEvents = false; // have we subscribed to NetworkServer events?
        static EnemyAI s_coroutineRunner = null;     // a single instance used to run coroutines (first server EnemyAI)
        public override void OnStartServer()
        {
            base.OnStartServer();

            // track number of server-side EnemyAI instances so we subscribe/unsubscribe exactly once
            s_serverEnemyCount++;

            // choose first server EnemyAI as the coroutine runner
            if (s_coroutineRunner == null)
                s_coroutineRunner = this;

            // subscribe once for server connection events
            if (!s_subscribedToNetEvents)
            {
                NetworkServer.OnConnectedEvent += OnServerConnected;
                NetworkServer.OnDisconnectedEvent += OnServerDisconnected;
                s_subscribedToNetEvents = true;
            }

            // existing start logic (only server-side behavior remains valid)
            _entity = GetComponent<Entity>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = StoppingDistance;
            // ... (keep other Start initialization here; omitted for brevity)
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            // decrement server instance count and unsubscribe if last enemy destroyed/disabled on server
            s_serverEnemyCount--;
            if (s_serverEnemyCount <= 0 && s_subscribedToNetEvents)
            {
                NetworkServer.OnConnectedEvent -= OnServerConnected;
                NetworkServer.OnDisconnectedEvent -= OnServerDisconnected;
                s_subscribedToNetEvents = false;
                s_coroutineRunner = null;
            }
        }

        void OnDestroy()
        {
            // ensure unsubscribe if object destroyed while server count reaches 0
            if (isServer)
                OnStopServer();
        }

        // Called when a connection arrives. The player's GameObject (connection.identity) may not yet be set,
        // so we wait briefly (up to a timeout) for it to be created and then register the player's Entity.
        static void OnServerConnected(NetworkConnectionToClient conn)
        {
            if (conn == null) return;

            // if identity already exists, register immediately
            if (conn.identity != null)
            {
                TryRegisterEntityFromIdentity(conn.identity);
                return;
            }

            // otherwise we need to wait a frame or two until the player object is created.
            // we rely on a server-side EnemyAI instance to run the coroutine. If none exists we skip —
            // prefer registering from the player's OnStartServer for full reliability.
            if (s_coroutineRunner != null)
            {
                s_coroutineRunner.StartCoroutine(s_coroutineRunner.WaitForIdentityAndRegister(conn));
            }
            else
            {
                Debug.LogWarning("[EnemyAI] No server EnemyAI instance available to wait for new player's identity. Consider registering players from player OnStartServer.");
            }
        }

        // Called when a connection disconnects — remove any registered player entities associated with that connection.
        static void OnServerDisconnected(NetworkConnectionToClient conn)
        {
            if (conn == null) return;

            // If the identity still exists, unregister it quickly
            if (conn.identity != null)
            {
                var ent = conn.identity.GetComponent<Entity>();
                if (ent != null) UnregisterPlayer(ent);
            }

            // Further cleanup: remove any entries by connection match (safety)
            PlayerEntities.RemoveAll(e =>
            {
                if (e == null) return true;
                var nid = e.GetComponent<NetworkIdentity>();
                return nid == null ? false : nid.connectionToClient == conn;
            });
        }

        // Coroutine used to wait until the player's identity is assigned (or until timeout)
        IEnumerator WaitForIdentityAndRegister(NetworkConnectionToClient conn)
        {
            const float timeout = 5f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (conn == null) yield break;
                if (conn.identity != null) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (conn == null || conn.identity == null) yield break;

            TryRegisterEntityFromIdentity(conn.identity);
        }

        static void TryRegisterEntityFromIdentity(NetworkIdentity nid)
        {
            if (nid == null) return;
            var ent = nid.GetComponent<Entity>();
            if (ent == null) return;

            RegisterPlayer(ent);
        }

        public static void RegisterPlayer(Entity e)
        {
            if (e == null) return;
            if (!PlayerEntities.Contains(e))
            {
                PlayerEntities.Add(e);
                Debug.Log($"[EnemyAI] Registered player entity: {e.name}");
            }
        }

        public static void UnregisterPlayer(Entity e)
        {
            if (e == null) return;
            if (PlayerEntities.Contains(e))
            {
                PlayerEntities.Remove(e);
                Debug.Log($"[EnemyAI] Unregistered player entity: {e.name}");
            }
        }

        #endregion
        // ---------------------------
        // Enemy lifecycle
        // ---------------------------

        void Start()
        {
            _entity = GetComponent<Entity>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = StoppingDistance;

            // Apply archetype overrides if present (kept from your original code)
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

            // Resolve modular components (unchanged)
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

            // Try to find an initial player target:
            // Prefer registered players list; fallback to EntityManager or tag lookup.
            _target = GetClosestPlayerFromRegistry();
            if (_target == null && EntityManager.Instance != null)
            {
                _target = EntityManager.Instance.GetClosest(transform.position, PlayerTag);
            }
            if (_target == null && !string.IsNullOrEmpty(PlayerTag))
            {
                var go = GameObject.FindWithTag(PlayerTag);
                if (go != null) _target = go.GetComponent<Entity>();
            }
        }

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

            // Clean up any null entries in the static player list (players that disconnected/destroyed)
            if (PlayerEntities.Count > 0)
                PlayerEntities.RemoveAll(x => x == null);

            // Acquire or refresh the closest player target from the registry if needed
            if (_target == null || !PlayerEntities.Contains(_target))
            {
                _target = GetClosestPlayerFromRegistry();
            }

            // Fall back to your previous mechanisms if still no target
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

            if (inAggroRange)
            {
                _aggroTimer = AggroDuration;
            }

            if (_aggroTimer > 0f)
            {
                if (_movementController != null)
                {
                    if (!stunned)
                        _movementController.MoveTo(_target.transform.position);
                    else
                        _movementController.Stop();
                }
                else
                {
                    if (_agent.isOnNavMesh)
                    {
                        if (!_agent.enabled) _agent.enabled = true;
                        if (!stunned && _agent.isStopped) _agent.isStopped = false;

                        _agent.SetDestination(_target.transform.position);
                    }
                    else
                    {
                        Debug.Log("Agent is not on a NavMesh");
                    }
                }

                if (distSqr <= AttackRange * AttackRange && _attackTimer <= 0f)
                {
                    bool attacked = false;
                    if (_attackHandler != null)
                    {
                        attacked = _attackHandler.TryAttack(_target);
                    }
                    else
                    {
                        attacked = TryAttackTargetFallback();
                    }

                    if (attacked)
                    {
                        _attackTimer = AttackCooldown;
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
            }
        }

        // Returns the closest registered player Entity or null if none
        Entity GetClosestPlayerFromRegistry()
        {
            if (PlayerEntities.Count == 0) return null;

            Entity best = null;
            float bestSqr = float.MaxValue;
            Vector3 pos = transform.position;

            for (int i = 0; i < PlayerEntities.Count; i++)
            {
                var p = PlayerEntities[i];
                if (p == null) continue; // cleaned periodically anyway
                float d = (p.transform.position - pos).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = p;
                }
            }

            return best;
        }

        // fallback direct attack logic (unchanged)
        bool TryAttackTargetFallback()
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