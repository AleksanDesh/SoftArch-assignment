using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    public class DungeonManager : MonoBehaviour
    {
        [Tooltip("Room prefabs and corridor prefabs in alternating order (Room, Corridor, Room, Corridor, etc).")]
        public GameObject[] SequencePrefabs;

        [Tooltip("Parent used for instantiated rooms and corridors")]
        public Transform RoomsParent;

        [Tooltip("Delay between door closing and spawning next set")]
        public float TransitionDelay = 0.8f;

        int _currentIndex = -1;

        // Current active room
        Room _currentRoom;
        GameObject _currentRoomGO;

        // Currently active corridor between rooms
        GameObject _currentCorridorGO;

        // Pending next room (spawned but not yet entered)
        GameObject _pendingRoomGO;
        Room _pendingRoom;

        void Start()
        {
            if (SequencePrefabs == null || SequencePrefabs.Length == 0)
            {
                Debug.LogWarning("DungeonManager: No prefabs assigned.");
                return;
            }

            StartAt(0);
        }

        public void StartAt(int index)
        {
            if (index < 0 || index >= SequencePrefabs.Length)
            {
                Debug.LogWarning("StartAt: invalid index");
                return;
            }

            // Cleanup any existing content
            if (_currentRoomGO != null)
            {
                _currentRoom?.Deactivate();
                Destroy(_currentRoomGO);
                _currentRoomGO = null;
                _currentRoom = null;
            }

            _currentIndex = index;
            SpawnRoomAtIndex(_currentIndex, out _currentRoomGO, out _currentRoom);
            _currentRoom.Activate();
            _currentRoom.OpenEntrance();
        }

        // Called by a corridor trigger when player enters corridor
        public void RequestRoomEntranceOpen(CorridorTrigger corridor)
        {
            if (_pendingRoom != null)
            {
                Debug.Log($"DungeonManager: Opening entrance for next room {_pendingRoom.name}");
                _pendingRoom.OpenEntrance();
            }
        }

        // Called by room when all enemies are dead
        public void NotifyRoomCleared(Room clearedRoom)
        {
            if (clearedRoom == null || clearedRoom != _currentRoom)
                return;

            int corridorIndex = _currentIndex + 1;
            int nextRoomIndex = _currentIndex + 2;

            if (nextRoomIndex >= SequencePrefabs.Length)
            {
                Debug.Log("DungeonManager: Dungeon completed! No more rooms.");
                return;
            }

            // spawn the corridor first
            if (corridorIndex < SequencePrefabs.Length)
            {
                var corridorPrefab = SequencePrefabs[corridorIndex];
                _currentCorridorGO = Instantiate(corridorPrefab, RoomsParent);
                _currentCorridorGO.name = $"{corridorPrefab.name}_{corridorIndex}";
            }

            // spawn the next room
            SpawnRoomAtIndex(nextRoomIndex, out _pendingRoomGO, out _pendingRoom);
            _pendingRoom.Activate();
        }

        // Called by room when players are inside
        public void NotifyPlayersInside(Room newRoom)
        {
            if (newRoom == null || newRoom != _pendingRoom)
                return;

            Debug.Log($"DungeonManager: Players entered room {newRoom.name}");

            // Unload last room and corridor
            if (_currentRoomGO != null)
            {
                _currentRoom.Deactivate();
                Destroy(_currentRoomGO);
            }
            if (_currentCorridorGO != null)
            {
                Destroy(_currentCorridorGO);
            }

            // promote pending room to current
            _currentRoomGO = _pendingRoomGO;
            _currentRoom = _pendingRoom;
            _currentIndex += 2; // because we advanced by corridor + room

            _pendingRoomGO = null;
            _pendingRoom = null;
        }

        void SpawnRoomAtIndex(int index, out GameObject roomGO, out Room room)
        {
            var prefab = SequencePrefabs[index];
            roomGO = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation, RoomsParent);
            roomGO.name = $"{prefab.name}_{index}";
            room = roomGO.GetComponent<Room>();
            if (room == null)
                room = roomGO.AddComponent<Room>();
        }
    }
}
