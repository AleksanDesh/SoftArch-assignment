using UnityEngine;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Types;

namespace DungeonCrawler.Systems.Movement
{
    /// <summary>
    /// Movement abstraction for enemies or other actors that need navmesh-based movement.
    /// Implementations should control NavMeshAgent / Rigidbody / custom movement.
    /// </summary>
    public interface IMovementController
    {
        /// <summary>Initialize controller (pass owning entity & archetype data).</summary>
        void Initialize(Entity owner, EnemyType archetype);

        /// <summary>Tell controller to move towards world position (may sample navmesh).</summary>
        void MoveTo(Vector3 worldPosition);

        /// <summary>Stop moving immediately (idle).</summary>
        void Stop();

        /// <summary>True while movement is happening.</summary>
        bool IsMoving { get; }

        /// <summary>Optional: current destination (or null/Vector3.zero if none).</summary>
        Vector3 CurrentDestination { get; }
    }
}
