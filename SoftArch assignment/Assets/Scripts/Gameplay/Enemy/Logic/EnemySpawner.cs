using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    /// <summary>
    /// Small helper that spawns enemy prefabs. Kept intentionally simple so I can
    /// replace with object-pool.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Optional parent for spawned enemies")]
        public Transform SpawnParent;

        /// <summary>Spawn one enemy prefab at world position. Returns the spawned GameObject.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, position, rotation, SpawnParent);
            return go;
        }

        /// <summary>Spawn multiple of same prefab using positions generator.</summary>
        public List<GameObject> SpawnMany(GameObject prefab, IEnumerable<Vector3> positions)
        {
            var list = new List<GameObject>();
            foreach (var p in positions)
            {
                var go = Spawn(prefab, p, Quaternion.identity);
                if (go != null) list.Add(go);
            }
            return list;
        }
    }
}
