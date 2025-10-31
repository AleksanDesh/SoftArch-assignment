using UnityEngine;
using UnityEngine.AI;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Types;

namespace DungeonCrawler.Systems.Movement
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshMovementController : MonoBehaviour, IMovementController
    {
        NavMeshAgent _agent;
        Entity _owner;
        MeleeEnemyType _archetype;
        Vector3 _currentDest = Vector3.zero;

        public bool IsMoving => _agent != null && _agent.hasPath && !_agent.isStopped;
        public Vector3 CurrentDestination => _currentDest;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(Entity owner, MeleeEnemyType archetype)
        {
            Debug.Log("Using custom movement logic Melee");
            _owner = owner;
            _archetype = archetype;

            if (_agent == null) _agent = GetComponent<NavMeshAgent>();

            if (_agent != null && archetype != null)
            {
                _agent.speed = archetype.MoveSpeed;
                _agent.acceleration = archetype.Acceleration;
                _agent.stoppingDistance = archetype.StoppingDistance;
                _agent.enabled = true;

                // Ensure agent sits on NavMesh; try to snap if not
                if (!_agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                    {
                        _agent.Warp(hit.position);
                    }
                }
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            if (_agent == null) return;

            Vector3 dest = worldPosition;

            if (_archetype != null && _archetype.SampleTargetPositionOnNavMesh)
            {
                if (NavMesh.SamplePosition(worldPosition, out var hit, 1.0f, NavMesh.AllAreas))
                    dest = hit.position;
            }

            _currentDest = dest;

            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }
            else
            {
                // Try to sample agent position and warp if possible
                if (NavMesh.SamplePosition(transform.position, out var agentHit, 2f, NavMesh.AllAreas))
                {
                    _agent.Warp(agentHit.position);
                    _agent.SetDestination(dest);
                }
            }
        }

        public void Stop()
        {
            if (_agent == null) return;
            _agent.ResetPath();
            _agent.isStopped = true;
            _currentDest = transform.position;
        }
    }
}
