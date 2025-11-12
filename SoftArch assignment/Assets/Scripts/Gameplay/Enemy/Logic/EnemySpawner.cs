using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Enemy.Logic
{
    /// <summary>
    /// Small helper that spawns enemy prefabs. Kept intentionally simple so I can
    /// replace with object-pool.
    /// </summary>
    public class EnemySpawner : NetworkBehaviour
    {
    //    [Tooltip("Optional parent for spawned enemies")]
    //    public Transform SpawnParent;

    //    /// <summary>Spawn one enemy prefab at world position. Returns the spawned GameObject.</summary>
    //    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    //    {
    //        if (prefab == null) return null;
    //        GameObject go = Instantiate(prefab, position, rotation);

    //        if (!NetworkServer.active)
    //        {
    //            Debug.LogError($"EnemySpawner.Spawn called but NetworkServer not active! prefab={prefab.name}");
    //        }

    //        NetworkServer.Spawn(go);

    //        //Debug.Log($"[EnemySpawner] Spawned prefab={prefab.name} netId={(go.GetComponent<NetworkIdentity>()?.netId ?? 0)}");

    //        return go;
    //    }

    //    /// <summary>Spawn multiple of same prefab using positions generator.</summary>
    //    public List<GameObject> SpawnMany(GameObject prefab, IEnumerable<Vector3> positions)
    //    {
    //        var list = new List<GameObject>();
    //        foreach (var p in positions)
    //        {
    //            var go = Spawn(prefab, p, Quaternion.identity);
    //            if (go != null) list.Add(go);
    //        }
    //        return list;
    //    }
    }
}
