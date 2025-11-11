using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Logic;
using KinematicCharacterController;
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
    public class Room : MonoBehaviour
    {
        [Header("Doors (door 0 = entrance)")]
        public GameObject[] Doors;
        public DoorAnimator DoorAnimator;

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

        void Reset()
        {
            FindSpawnPoints();
            Spawner = GetComponentInChildren<EnemySpawner>();
            DoorAnimator = GetComponentInChildren<DoorAnimator>();
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

            if (DoorAnimator == null)
                DoorAnimator = GetComponentInChildren<DoorAnimator>();
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

        /// <summary>Activate room: open entrance, spawn enemies, start close-timer.</summary>
        public void Activate()
        {
            if (activated) return;
            activated = true;

            SubscribeDeath();

            if (SpawnOnActivate) SpawnEnemies();
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
            if (sp == null) return;
            if (SpawnPoints == null || SpawnPoints.Length == 0)
                FindSpawnPoints();

            spawnedEntityIds.Clear();

            foreach (var spoint in SpawnPoints)
            {
                if (spoint == null || spoint.GetPrefab() == null || spoint.Quantity <= 0) continue;

                var positions = new List<Vector3>(spoint.Quantity);
                for (int i = 0; i < spoint.Quantity; i++) positions.Add(spoint.GetSpawnPosition());

                var spawned = sp.SpawnMany(spoint.GetPrefab(), positions);
                if (spawned == null) continue;

                // record entity ids for each spawned enemy (only if they have Entity component)
                foreach (var go in spawned)
                {
                    if (go == null) continue;
                    var ent = go.GetComponent<Entity>();
                    if (ent != null) spawnedEntityIds.Add(ent.Id);
                }
            }

            // if nothing spawned, room is already cleared
            if (spawnedEntityIds.Count == 0) RoomCleared();
        }

        // --- DeathEvent handler -------------------------------------------------

        void SubscribeDeath()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Subscribe<DeathEvent>(OnDeathEvent);
        }

        void UnsubscribeDeath()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<DeathEvent>(OnDeathEvent);
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

        public void OpenEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            if (DoorAnimator != null) DoorAnimator.OpenDoor(d); else SetDoorOpenFallback(d, true);

            if (entranceCoroutine != null) StopCoroutine(entranceCoroutine);
            entranceCoroutine = StartCoroutine(EntranceTimer());
        }

        void CloseEntrance()
        {
            if (Doors == null || Doors.Length == 0) return;
            var d = Doors[0];
            if (d == null) return;
            if (DoorAnimator != null) DoorAnimator.CloseDoor(d); else SetDoorOpenFallback(d, false);
        }

        public void CloseAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
                if (DoorAnimator != null) DoorAnimator.CloseDoor(d); else SetDoorOpenFallback(d, false);
            }
        }

        public void OpenAllExceptEntrance()
        {
            if (Doors == null) return;
            for (int i = 1; i < Doors.Length; i++)
            {
                var d = Doors[i];
                if (d == null) continue;
                if (DoorAnimator != null) DoorAnimator.OpenDoor(d); else SetDoorOpenFallback(d, true);
            }
        }

        void SetDoorOpenFallback(GameObject door, bool open)
        {
            // fallback semantics: when open==true -> door disabled (passage clear)
            if (door == null) return;
            door.SetActive(!open);
            var cols = door.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in cols) c.enabled = !open;
        }

        void TeleportPlayersNotInside()
        {
            // prefer EntityManager (if present) to enumerate players with Entity
            if (EntityManager.Instance != null)
            {
                foreach (var ent in EntityManager.Instance.GetEntitiesWithTag(PlayerTag))
                {
                    if (ent == null) continue;
                    // assume players are tagged or identified elsewhere; fallback: check tag
                    var go = ent.gameObject;
                    if (go == null) continue;
                    if (!go.CompareTag(PlayerTag)) continue;
                    if (IsInsideRoom(go.transform.position)) continue;
                    TeleportPlayer(go, EntranceFallbackPoint.position);
                }
            }
            else
            {
                var players = GameObject.FindGameObjectsWithTag(PlayerTag);
                foreach (var p in players)
                {
                    if (p == null) continue;
                    if (IsInsideRoom(p.transform.position)) continue;
                    TeleportPlayer(p, EntranceFallbackPoint.position);
                }
            }
            if (dm != null)
                dm.NotifyPlayersInside(this);
            else
                Debug.LogWarning("Dungeon Manager wasn't found. Ensure there is only one Dm in the scene");
        }

        void TeleportPlayer(GameObject player, Vector3 dest)
        {
            if (player == null) return;

            player.GetComponent<KinematicCharacterMotor>().SetPosition(dest);
        }

        bool IsInsideRoom(Vector3 worldPos)
        {
            if (RoomBounds != null) return RoomBounds.bounds.Contains(worldPos);
            // fallback: any collider bounds
            foreach (var c in GetComponentsInChildren<Collider>()) if (c.bounds.Contains(worldPos)) return true;
            return false;
        }
    }
}
