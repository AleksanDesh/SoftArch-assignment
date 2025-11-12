using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Logic;
using KinematicCharacterController;
using Mirror;
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
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(EnemySpawner))]
    [RequireComponent(typeof(DoorAnimator))]
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
        public RoomSpawnPoint[] SpawnPoints;
        public EnemySpawner Spawner;
        public bool SpawnOnActivate = true;

        // internal
        HashSet<int> spawnedEntityIds = new HashSet<int>();
        Coroutine entranceCoroutine;
        bool activated;
        DungeonManager dm;
        DoorAnimator doorAnimator;


        void Reset()
        {
            FindSpawnPoints();
            Spawner = GetComponentInChildren<EnemySpawner>();
            doorAnimator = GetComponent<DoorAnimator>();
        }

        void Awake()
        {
            // Ensure NetworkIdentity exists at runtime
            if (GetComponent<NetworkIdentity>() == null)
                gameObject.AddComponent<NetworkIdentity>();
        }

        void Start()
        {
            dm = Object.FindFirstObjectByType<DungeonManager>();
            if (SpawnPoints == null || SpawnPoints.Length == 0)
                FindSpawnPoints();

            if (RoomBounds == null)
            {
                // prefer an explicit trigger collider; fallback to any trigger found under children
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.isTrigger) { RoomBounds = c; break; }
            }

            if (doorAnimator == null)
                doorAnimator = GetComponentInChildren<DoorAnimator>();
        }

        void FindSpawnPoints()
        {
            // Look for a child object named "SpawnPositions"
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

            // fallback – get all in children if "SpawnPositions" doesn’t exist
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

        /// <summary>Activate room: open entrance, spawn enemies, start close-timer.
        /// MUST BE CALLED ONLY FROM SERVER SIDE!
        /// </summary>
        public void Activate()
        {
            if (activated) return;
            activated = true;

            // Subscribe to death events
            SubscribeDeath();

            // Spawn on next frame to ensure Room.Start() and other initialisation ran
            // (prevents races where SpawnPoints/Spawner hasn't yet been discovered)
            StartCoroutine(ActivateNextFrame());
        }

        IEnumerator ActivateNextFrame()
        {
            yield return null; // wait one frame

            if (SpawnOnActivate) SpawnEnemies();

            // Open entrance after spawn (don't do it here)
            // OpenEntrance();
        }

        /// <summary>Deactivate room: cancel timers and unsubscribe</summary>
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

        // --- Spawning & tracking ------------------------------------------------

        public void SpawnEnemies()
        {
                var sp = Spawner ?? GetComponentInChildren<EnemySpawner>();
            if (sp == null) { Debug.LogWarning($"{name}: No EnemySpawner found."); return; }
            if (SpawnPoints == null || SpawnPoints.Length == 0) FindSpawnPoints();

            spawnedEntityIds.Clear();

            Debug.Log($"{name}: Server spawning enemies (spawn points: {SpawnPoints?.Length ?? 0})");

            foreach (var spoint in SpawnPoints)
            {
                if (spoint == null || spoint.GetPrefab() == null || spoint.Quantity <= 0) continue;

                // Verify prefab is registered in NetworkManager.spawnPrefabs
                var prefab = spoint.GetPrefab();
                if (NetworkManager.singleton != null && !NetworkManager.singleton.spawnPrefabs.Contains(prefab))
                {
                    Debug.LogError($"{name}: Prefab {prefab.name} is NOT registered in NetworkManager.spawnPrefabs. Add it there or NetworkServer.Spawn will fail.");
                }

                var positions = new List<Vector3>(spoint.Quantity);
                for (int i = 0; i < spoint.Quantity; i++) positions.Add(spoint.GetSpawnPosition());

                var spawned = sp.SpawnMany(prefab, positions);
                if (spawned == null) continue;

                Debug.Log($"{name}: Spawned {spawned.Count} instances of {prefab.name}.");

                foreach (var go in spawned)
                {
                    if (go == null) continue;
                    var ent = go.GetComponent<Entity>();
                    if (ent != null) spawnedEntityIds.Add(ent.Id);
                }
            }

            if (spawnedEntityIds.Count == 0)
            {
                //Debug.Log($"{name}: No enemies spawned — marking room cleared.");
                RoomCleared();
            }
            else
            {
                //Debug.Log($"{name}: SpawnedEntityIds count = {spawnedEntityIds.Count}");
            }
        }

        // --- DeathEvent handler -------------------------------------------------

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
            // only remove if it belonged to this room
            if (spawnedEntityIds.Remove(deadId))
            {
                // immediate room-clear check
                if (spawnedEntityIds.Count == 0)
                {
                    RoomCleared();
                }
            }
        }

        void RoomCleared()
        {
            if (dm != null)
                dm.NotifyRoomCleared(this);
            else
                Debug.LogWarning("Dungeon Manager wasn't found. Ensure there is only one Dm in the scene. Or the room didn't spawn enemies");

            // Open all doors except entrance (index 0)
            OpenAllExceptEntrance();

            UnsubscribeDeath();
        }

        // --- Door helpers ------------------------------------------------------
        [Server]
        public void OpenEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            doorAnimator.OpenDoor(d);

            if (entranceCoroutine != null) StopCoroutine(entranceCoroutine);
            entranceCoroutine = StartCoroutine(EntranceTimer());
        }

        void CloseEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            doorAnimator.CloseDoor(d);
        }

        public void CloseAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
                doorAnimator.CloseDoor(d);
            }
        }

        public void OpenAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
               doorAnimator.OpenDoor(d);
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

            // iterate all connected players on server (authoritative)
            foreach (var kv in NetworkServer.connections)
            {
                var conn = kv.Value;
                if (conn == null || conn.identity == null) continue;

                var playerGO = conn.identity.gameObject;
                if (playerGO == null) continue;

                // if player is inside, skip
                //Debug.LogWarning($"{IsPlayerInside(playerGO)} inside bounds player {playerGO.name}");
                if (IsPlayerInside(playerGO))
                    continue;

                // teleport player
                var playerNet = conn.identity.GetComponent<PlayerNet>();
                if (playerNet != null)
                    playerNet.TargetTeleport(conn, dest);
               
                //Debug.LogWarning($"Teleporting player {playerGO.name} netId={conn.identity.netId}");
                teleported++;
            }

            //Debug.Log($"{name}: Teleported {teleported} players to fallback.");

            if (dm != null) dm.NotifyPlayersInside(this);
            else Debug.LogWarning("Dungeon Manager wasn't found. Ensure there is only one Dm in the scene");
        }


        // checks using collider bounds intersection
        bool IsPlayerInside(GameObject player)
        {
            if (player == null) return false;

            Vector3 p = player.transform.position;

            if (RoomBounds != null)
            {
                // quick point containment test (more reliable across network)
                if (RoomBounds.bounds.Contains(p)) return true;

                // optional: also try colliders intersection if you really want
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


        //// New ClientRpc: sent to all clients, but only the owning client will act on it
        //[ClientRpc]
        //void RpcTeleportPlayer(uint playerNetId, Vector3 dest)
        //{
        //    // Debug to confirm call on client
        //    Debug.Log($"RpcTeleportPlayer received on client. playerNetId={playerNetId} myLocalNetId={(NetworkClient.connection != null && NetworkClient.connection.identity != null ? NetworkClient.connection.identity.netId : 0)} dest={dest}");

        //    // check if this client actually owns that netId
        //    if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
        //    {
        //        // no local player yet — nothing to do
        //        return;
        //    }

        //    if (NetworkClient.connection.identity.netId != playerNetId)
        //    {
        //        // not our player
        //        return;
        //    }

        //    // This is our local player — teleport it
        //    var go = NetworkClient.connection.identity.gameObject;
        //    if (go == null)
        //    {
        //        Debug.LogWarning("RpcTeleportPlayer: local identity has no gameObject.");
        //        return;
        //    }

        //    var kcm = go.GetComponent<KinematicCharacterMotor>();
        //    if (kcm != null)
        //    {
        //        kcm.SetPosition(dest);
        //        Debug.Log($"RpcTeleportPlayer: teleported local player (KCM) to {dest}");
        //        return;
        //    }

        //    go.transform.position = dest;
        //    Debug.Log($"RpcTeleportPlayer: teleported local player (transform) to {dest}");
        //}

        //// Updated server-side TeleportPlayer: set server-side state and notify clients by netId
        //void TeleportPlayer(GameObject player, Vector3 dest)
        //{
        //    if (player == null) return;
        //    Debug.Log($"Trying to teleport player {player.name} to {dest}");
        //    var ni = player.GetComponent<NetworkIdentity>();
        //    if (ni != null && ni.connectionToClient != null)
        //    {
        //        // Server: set server-side position first (so host sees immediate update)
        //        var kcmServer = player.GetComponent<KinematicCharacterMotor>();
        //        if (kcmServer != null)
        //        {
        //            kcmServer.SetPosition(dest);
        //        }
        //        else
        //        {
        //            var ccServer = player.GetComponent<CharacterController>();
        //            if (ccServer != null)
        //            {
        //                ccServer.enabled = false;
        //                player.transform.position = dest;
        //                ccServer.enabled = true;
        //            }
        //            else
        //            {
        //                player.transform.position = dest;
        //            }
        //        }

        //        Debug.Log($"TeleportPlayer (server): teleported server-side player netId={ni.netId} to {dest}");

        //        // Notify clients — the owning client will apply the local teleport
        //        RpcTeleportPlayer(ni.netId, dest);
        //        return;
        //    }

        //    // fallback: non-networked or server-authoritative object — move on server
        //    var kcm = player.GetComponent<KinematicCharacterMotor>();
        //    if (kcm != null) { kcm.SetPosition(dest); return; }
        //    player.transform.position = dest;
        //}

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
