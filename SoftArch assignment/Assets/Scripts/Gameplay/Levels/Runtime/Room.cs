using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Logic;
using KinematicCharacterController;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Minimal, focused Room behaviour:
    /// - open entrance (door 0) on Activate
    /// - close entrance after EntranceOpenDuration and teleport players not inside
    /// - spawn enemies and track them by Entity.Id (HashSet<int>)
    /// - react to DeathEvent and remove by entity id; when empty => room cleared
    /// - on cleared: open all doors except entrance and notify DungeonManager (optional)
    /// </summary>
    [DisallowMultipleComponent]
    public class Room : NetworkBehaviour
    {
        [Header("Doors (door 0 = entrance)")]
        public GameObject[] Doors;

        [Header("Entrance")]
        public float EntranceOpenDuration = 5f;
        public bool TeleportPlayersOnClose = true;
        public Transform EntranceFallbackPoint;
        public string PlayerTag = "Player"; // fallback player search

        [Header("Room bounds (used to determine who's inside)")]
        public Collider RoomBounds;

        [Header("Spawning")]
        [Tooltip("Optional parent for spawned enemies")]
        public Transform SpawnParent;
        public RoomSpawnPoint[] SpawnPoints;
        public bool SpawnOnActivate = true;

        public string OpenTrigger = "Open";
        public string CloseTrigger = "Close";

        // internal
        HashSet<int> spawnedEntityIds = new HashSet<int>();
        Coroutine entranceCoroutine;
        bool activated;
        DungeonManager dm;
        NetworkIdentity myIdentity;

        void Reset()
        {
            FindSpawnPoints();
        }

        private void Awake()
        {
            myIdentity = this.GetComponent<NetworkIdentity>();
        }

        void Start()
        {
            dm = UnityEngine.Object.FindFirstObjectByType<DungeonManager>();
            if (SpawnPoints == null || SpawnPoints.Length == 0)
                FindSpawnPoints();

            if (RoomBounds == null)
            {
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.isTrigger) { RoomBounds = c; break; }
            }
        }

        void FindSpawnPoints()
        {
            var spawnParent = transform.Find("SpawnPositions");
            if (spawnParent != null)
            {
                SpawnPoints = spawnParent.GetComponentsInChildren<RoomSpawnPoint>(includeInactive: true);
                if (SpawnPoints.Length > 0)
                {
                    Debug.Log($"{name}: Found {SpawnPoints.Length} spawn points under SpawnPositions.");
                    return;
                }
            }

            SpawnPoints = GetComponentsInChildren<RoomSpawnPoint>(includeInactive: true);
            if (SpawnPoints.Length > 0)
                Debug.Log($"{name}: Found {SpawnPoints.Length} spawn points (fallback).");
            else
                Debug.LogWarning($"{name}: No spawn points found!");
        }

        void OnDestroy()
        {
            UnsubscribeDeath();
        }

        public void Activate()
        {
            if (activated) return;
            activated = true;
            SubscribeDeath();
            StartCoroutine(ActivateNextFrame());
        }

        IEnumerator ActivateNextFrame()
        {
            yield return null;
            if (SpawnOnActivate) SpawnEnemies();
        }

        public void Deactivate()
        {
            if (!activated) return;
            activated = false;
            if (entranceCoroutine != null) { StopCoroutine(entranceCoroutine); entranceCoroutine = null; }
            UnsubscribeDeath();
        }

        IEnumerator EntranceTimer()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, EntranceOpenDuration));
            if (TeleportPlayersOnClose) TeleportPlayersNotInside();
            CloseEntrance();
            entranceCoroutine = null;
        }

        public void SpawnEnemies()
        {
            if (SpawnPoints == null || SpawnPoints.Length == 0) FindSpawnPoints();

            spawnedEntityIds.Clear();

            Debug.Log($"{name}: Server spawning enemies (spawn points: {SpawnPoints?.Length ?? 0})");

            foreach (var spoint in SpawnPoints)
            {
                if (spoint == null || spoint.GetPrefab() == null || spoint.Quantity <= 0) continue;

                var prefab = spoint.GetPrefab();
                if (NetworkManager.singleton != null && !NetworkManager.singleton.spawnPrefabs.Contains(prefab))
                {
                    Debug.LogError($"{name}: Prefab {prefab.name} is NOT registered in NetworkManager.spawnPrefabs. Add it there or NetworkServer.Spawn will fail.");
                }

                var positions = new List<Vector3>(spoint.Quantity);
                for (int i = 0; i < spoint.Quantity; i++) positions.Add(spoint.GetSpawnPosition());

                var spawned = SpawnMany(prefab, positions);
                if (spawned == null) continue;

                foreach (var go in spawned)
                {
                    if (go == null) continue;
                    var ent = go.GetComponent<Entity>();
                    if (ent != null) spawnedEntityIds.Add(ent.Id);
                }
            }

            if (spawnedEntityIds.Count == 0)
            {
                RoomCleared();
            }
        }

        void SubscribeDeath()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Subscribe<DeathEvent>(OnDeathEvent);
            }
        }

        void UnsubscribeDeath()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<DeathEvent>(OnDeathEvent);
            }
        }

        void OnDeathEvent(DeathEvent ev)
        {
            if (ev == null || ev.SourceEntity == null) return;
            int deadId = ev.SourceEntity.Id;
            if (spawnedEntityIds.Remove(deadId))
            {
                if (spawnedEntityIds.Count == 0) RoomCleared();
            }
        }

        void RoomCleared()
        {
            if (dm != null) dm.NotifyRoomCleared(this);
            else Debug.LogWarning("Dungeon Manager wasn't found. Ensure there is only one Dm in the scene. Or the room didn't spawn enemies");

            Debug.Log($"Room {this.name} was cleared, opening all exits");
            OpenAllExceptEntrance();
            UnsubscribeDeath();
        }

        [Server]
        public void OpenEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            OpenDoor(0);
            if (entranceCoroutine != null) StopCoroutine(entranceCoroutine);
            entranceCoroutine = StartCoroutine(EntranceTimer());
        }

        [Server]
        void CloseEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            CloseDoor(0);
        }

        [Server]
        public void CloseAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
                CloseDoor(i);
            }
        }

        [Server]
        public void OpenAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
                OpenDoor(i);
            }
        }

        void TeleportPlayersNotInside()
        {
            if (!isServer)
            {
                Debug.LogWarning($"{name}: TeleportPlayersNotInside called on non-server — ignoring.");
                return;
            }

            if (EntranceFallbackPoint == null)
            {
                Debug.LogWarning($"{name}: EntranceFallbackPoint is null. Cannot teleport players.");
                return;
            }

            var dest = EntranceFallbackPoint.position;
            var teleported = 0;

            foreach (var kv in NetworkServer.connections)
            {
                var conn = kv.Value;
                if (conn == null || conn.identity == null) continue;

                var playerGO = conn.identity.gameObject;
                if (playerGO == null) continue;

                if (IsPlayerInside(playerGO)) continue;

                var playerNet = conn.identity.GetComponent<PlayerNet>();
                if (playerNet != null)
                    playerNet.TargetTeleport(conn, dest);

                teleported++;
            }

            if (dm != null) dm.NotifyPlayersInside(this);
            else Debug.LogWarning("Dungeon Manager wasn't found. Ensure there is only one Dm in the scene");
        }

        bool IsPlayerInside(GameObject player)
        {
            if (player == null) return false;

            Vector3 p = player.transform.position;

            if (RoomBounds != null)
            {
                if (RoomBounds.bounds.Contains(p)) return true;

                var playerCols = player.GetComponentsInChildren<Collider>();
                if (playerCols != null)
                {
                    foreach (var pc in playerCols)
                    {
                        if (pc == null) continue;
                        if (RoomBounds.bounds.Intersects(pc.bounds)) return true;
                    }
                }
            }

            return false;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, position, rotation);

            if (!NetworkServer.active)
            {
                Debug.LogError($"EnemySpawner.Spawn called but NetworkServer not active! prefab={prefab.name}");
            }

            NetworkServer.Spawn(go);

            return go;
        }

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

        #region Door logic

        [Server]
        public void OpenDoor(int index)
        {
            StartCoroutine(OpenEntranceNextFrame(index));
        }
        IEnumerator OpenEntranceNextFrame(int index)
        {
            yield return new WaitForEndOfFrame();
            GameObject door = Doors[index];
            Debug.Log($"{name}: observers count = {netIdentity.observers.Count}");
            Debug.Log($"[Server] Calling RpcSetDoorState on {door.name} (room active={gameObject.activeSelf}, enabled={enabled}, netId={netIdentity?.netId})");
            if (door == null) yield return null;
            RpcSetDoorState(index, true);

            if (!isServer) yield return null;
            if (myIdentity == null) myIdentity = GetComponentInParent<NetworkIdentity>();
            if (myIdentity == null)
            {
                Debug.LogWarning($"{name}: No NetworkIdentity found — cannot sync door '{door.name}'. Applying locally only.");
                yield return null;
            }
        }

        [Server]
        public void CloseDoor(int index)
        {
            GameObject door = Doors[index];
            Debug.Log($"{name}: observers count = {netIdentity.observers.Count}");
            Debug.Log($"[Server] Calling RpcSetDoorState on {door.name} (room active={gameObject.activeSelf}, enabled={enabled}, netId={netIdentity?.netId})");
            if (door == null) return;
            RpcSetDoorState(index, false);

            if (!isServer) return;
            if (myIdentity == null) myIdentity = GetComponentInParent<NetworkIdentity>();
            if (myIdentity == null)
            {
                Debug.LogWarning($"{name}: No NetworkIdentity found — cannot sync door '{door.name}'. Applying locally only.");
                return;
            }
        }
        [ClientRpc]
        public void RpcSetDoorState(int index, bool open)
        {
            Debug.Log("RPC RECEIVED");
            if (Doors == null || index < 0 || index >= Doors.Length)
                return;

            if (open)
                OpenDoorLocal(index);
            else
                CloseDoorLocal(index);
        }


        void OpenDoorLocal(int index)
        {
            GameObject door = Doors[index];
            Debug.Log($"{name} Opening Door locally: {door?.name}");
            if (door == null) return;

            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                if (!door.activeInHierarchy) door.SetActive(true);
                animator.ResetTrigger(CloseTrigger);
                animator.SetTrigger(OpenTrigger);
            }
            else SetDoorActiveAndColliders(door, false);
        }

        void CloseDoorLocal(int index)
        {
            GameObject door = Doors[index];
            if (door == null) return;
            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                if (!door.activeInHierarchy) door.SetActive(true);
                animator.ResetTrigger(OpenTrigger);
                animator.SetTrigger(CloseTrigger);
            }
            else SetDoorActiveAndColliders(door, true);
        }

        void SetDoorActiveAndColliders(GameObject door, bool active)
        {
            door.SetActive(active);
            var colliders = door.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in colliders) c.enabled = active;
        }

        #endregion

        #region NetworkResolving
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer) enabled = false;
        }
        #endregion
    }
}
