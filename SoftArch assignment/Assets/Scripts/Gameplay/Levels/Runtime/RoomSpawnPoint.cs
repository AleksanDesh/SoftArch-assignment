using UnityEngine;
using System.Collections.Generic;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Put this on empty GameObjects inside a Room prefab to mark spawn positions.
    /// Configure which enemy prefab to spawn and how many instances.
    /// </summary>
    public class RoomSpawnPoint : MonoBehaviour
    {
        [Tooltip("Prefab of enemy to spawn at this spawn point.")]
        public List<GameObject> EnemyPrefabs = new List<GameObject>();

        [Tooltip("How many to spawn at this point")]
        public int Quantity = 1;

        [Tooltip("Optional random positional jitter around this point")]
        public float JitterRadius = 0.3f;

        /// <summary>Generate a spawn position near this spawn point (with jitter).</summary>
        public Vector3 GetSpawnPosition()
        {
            Vector2 rnd = Random.insideUnitCircle * JitterRadius;
            return transform.position + new Vector3(rnd.x, 0f, rnd.y);
        }

        public GameObject GetPrefab()
        {
            if (EnemyPrefabs.Count == 0) return null;
            return EnemyPrefabs[Random.Range(0, EnemyPrefabs.Count)]; 
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, JitterRadius);
        }
    }
}
