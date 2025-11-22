using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    public class DungeonManager : NetworkBehaviour
    {
        [Header("Dungeon setup")]
        [Tooltip("List of room and corridor prefabs in the order they appear (Room, Corridor, Room, Corridor, ...)")]
        public List<GameObject> RoomPrefabs = new List<GameObject>();

        [Tooltip("Optional positional offset for spawned rooms.")]
        public Transform Offset;

        [Tooltip("Delay between transitions.")]
        public float TransitionDelay = 0.8f;

        // spawn-time instances; index matches RoomPrefabs. null = not spawned currently.
        private readonly List<GameObject> _spawnedInstances = new List<GameObject>();
        private int _currentIndex = -1;

        private Room _currentRoom;
        private GameObject _currentRoomGO;
        private GameObject _currentCorridorGO;
        private Room _pendingRoom;
        private GameObject _pendingRoomGO;

        // --------------------- SERVER INITIALIZATION ---------------------
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (RoomPrefabs == null || RoomPrefabs.Count == 0)
            {
                Debug.LogError("DungeonManager: RoomPrefabs list is empty!");
                return;
            }

            // prepare spawned instances list (all null initially)
            _spawnedInstances.Clear();
            for (int i = 0; i < RoomPrefabs.Count; i++)
                _spawnedInstances.Add(null);

            // start at 0 next frame (ensures clients are ready to receive spawn messages)
            StartCoroutine(ActivateNextFrame());
        }

        IEnumerator ActivateNextFrame()
        {
            yield return new WaitForEndOfFrame();
            StartAt(0);
        }

        // --------------------- DUNGEON FLOW ---------------------
        [Server]
        public void StartAt(int index = 0)
        {
            if (index < 0 || index >= RoomPrefabs.Count)
            {
                Debug.LogWarning("DungeonManager: Invalid start index.");
                return;
            }

            _currentIndex = index;

            // spawn the starting room
            SpawnAtIndex(_currentIndex, true, out _currentRoomGO, out _currentRoom);

            if (_currentRoom != null)
            {
                _currentRoom.Activate();
                _currentRoom.OpenEntrance();
            }
        }

        [Server]
        public void NotifyRoomCleared(Room clearedRoom)
        {
            if (clearedRoom == null || clearedRoom != _currentRoom) return;

            int corridorIndex = _currentIndex + 1;
            int nextRoomIndex = _currentIndex + 2;

            if (nextRoomIndex >= RoomPrefabs.Count)
            {
                Debug.Log("DungeonManager: Dungeon completed!");
                return;
            }

            if (corridorIndex < RoomPrefabs.Count)
            {
                // spawn corridor instead of enabling
                SpawnAtIndex(corridorIndex, true, out _currentCorridorGO, out _);
                Debug.Log($"Spawned corridor [{corridorIndex}] {_currentCorridorGO?.name}");
            }

            // spawn the pending room
            SpawnAtIndex(nextRoomIndex, true, out _pendingRoomGO, out _pendingRoom);
            _pendingRoom?.Activate();
        }

        [Server]
        public void NotifyPlayersInside(Room newRoom)
        {
            if (newRoom == null || newRoom != _pendingRoom) return;

            // destroy previous room (despawn) and corridor (despawn)
            if (_currentRoomGO != null)
            {
                int toDestroyIndex = _currentIndex;
                DestroyAtIndex(toDestroyIndex);
            }

            if (_currentCorridorGO != null)
            {
                int corridorIndex = _currentIndex + 1;
                DestroyAtIndex(corridorIndex);
                _currentCorridorGO = null;
            }

            // now apply pending -> current
            _currentRoomGO = _pendingRoomGO;
            _currentRoom = _pendingRoom;
            _currentIndex += 2;

            _pendingRoomGO = null;
            _pendingRoom = null;
        }

        [Server]
        public void RequestRoomEntranceOpen(CorridorTrigger corridor)
        {
            _pendingRoom?.OpenEntrance();
        }

        // --------------------- SPAWN / DESPAWN HELPERS ---------------------
        // Spawns the prefab at index if not already spawned. Returns spawned instance and Room component if requested.
        [Server]
        void SpawnAtIndex(int index, bool setCorridorActiveOnServer, out GameObject spawnedGO, out Room room)
        {
            spawnedGO = null;
            room = null;

            if (index < 0 || index >= RoomPrefabs.Count) return;
            if (RoomPrefabs[index] == null) return;

            if (_spawnedInstances[index] != null)
            {
                spawnedGO = _spawnedInstances[index];
                room = spawnedGO.GetComponent<Room>();
                return; // already spawned
            }

            var prefab = RoomPrefabs[index];
            var position = prefab.transform.position;
            if (Offset != null)
                position += Offset.position;
            GameObject instance = Instantiate(prefab, position, prefab.transform.rotation);

            NetworkServer.Spawn(instance);

            _spawnedInstances[index] = instance;
            spawnedGO = instance;
            room = instance.GetComponent<Room>();
        }

        // Destroys (despawns) the spawned instance at index if present.
        [Server]
        void DestroyAtIndex(int index)
        {
            if (index < 0 || index >= _spawnedInstances.Count) return;
            var instance = _spawnedInstances[index];
            if (instance == null) return;

            // If the Room has cleanup logic, call it before destroy
            var roomComp = instance.GetComponent<Room>();
            roomComp?.Deactivate();

            NetworkServer.Destroy(instance);
            _spawnedInstances[index] = null;
        }

        // --------------------- NETWORK SYNC HELPERS ---------------------
        // Keep SetChildObjectState / RPC for toggling child objects (doors, etc.)
        [Server]
        public void SetChildObjectState(GameObject parent, string relativePath, bool active)
        {
            if (parent == null) return;
            var ni = parent.GetComponent<NetworkIdentity>();
            if (ni == null) return;

            Transform child = parent.transform.Find(relativePath);
            if (child != null)
                child.gameObject.SetActive(active);

            RpcSetObjectState(ni.netId, relativePath, active);
        }

        [ClientRpc]
        void RpcSetObjectState(uint netId, string relativePath, bool active)
        {
            if (!NetworkClient.spawned.TryGetValue(netId, out var identity))
                return;

            Transform target = identity.transform;
            if (!string.IsNullOrEmpty(relativePath))
            {
                var found = identity.transform.Find(relativePath);
                if (found != null)
                    target = found;
            }

            if (target != null)
                target.gameObject.SetActive(active);
        }
    }
}
