using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Simple linear dungeon manager. Holds a list of room prefabs (or you can load them from scriptable LevelDescriptor).
    /// Spawns the first room at start (or via StartAtIndex) and handles transitions requested by corridor triggers.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class DungeonManager : MonoBehaviour
    {

        [Tooltip("Room prefabs, ordered by sequence in the dungeon")]
        public GameObject[] RoomPrefabs;

        [Tooltip("Parent used for instantiated rooms")]
        public Transform RoomsParent;

        [Tooltip("Delay (seconds) between closing doors and actually loading next room")]
        public float TransitionDelay = 0.8f;

        int _currentIndex = -1;
        Room _currentRoom;
        GameObject _currentRoomGO;


        // room that was spawned as the "next" while waiting for players to enter
        GameObject _pendingRoomGO;
        Room _pendingRoom;

        void Start()
        {
            if (RoomPrefabs == null || RoomPrefabs.Length == 0) { Debug.LogWarning("DungeonManager: no RoomPrefabs assigned."); return; }
            StartAt(0);
        }

        public void RequestRoomEntranceOpen(CorridorTrigger corridor)
        {
            // Called when corridor detects player entry and wants to open the entrance of the next room
            if (_pendingRoom != null)
            {
                Debug.Log($"DungeonManager: Opening entrance for next room {_pendingRoom.name}");
                _pendingRoom.OpenEntrance();
            }
        }

        public void NotifyRoomCleared(Room clearedRoom)
        {
            // ensure the cleared room is the current one
            if (clearedRoom == null || clearedRoom != _currentRoom)
                return;

            int nextIndex = _currentIndex + 1;
            if (nextIndex >= RoomPrefabs.Length)
            {
                Debug.Log("DungeonManager: no next room (end of dungeon).");
                return;
            }

            // Spawn next room and keep both current and pending alive
            var prefab = RoomPrefabs[nextIndex];
            _pendingRoomGO = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation, RoomsParent);
            _pendingRoomGO.name = $"{prefab.name}_{nextIndex}";
            _pendingRoom = _pendingRoomGO.GetComponent<Room>() ?? _pendingRoomGO.AddComponent<Room>();

            // Activate next room (it is responsible for opening entrance, teleporting players,
            // and eventually calling NotifyPlayersInside(this) when players are in)
            _pendingRoom.Activate();

            // Do nothing else — we wait until next room tells us players are inside.
        }

        // Called by the (new) room when it has teleported players and confirmed they are inside.
        public void NotifyPlayersInside(Room roomThatHasPlayersInside)
        {
            // only act if this is the pending room we spawned
            if (roomThatHasPlayersInside == null || roomThatHasPlayersInside != _pendingRoom)
                return;

            // safe unload of the previous (old) room
            if (_currentRoomGO != null)
            {
                // ask room to clean up, then destroy
                _currentRoom.Deactivate();
                Destroy(_currentRoomGO);
            }

            // promote pending to current
            _currentRoomGO = _pendingRoomGO;
            _currentRoom = _pendingRoom;
            _currentIndex++;

            // clear pending
            _pendingRoomGO = null;
            _pendingRoom = null;

            // optional: let others know a new room is fully entered (you can hook this up to EventBus if you want)
            Debug.Log($"DungeonManager: players entered room {_currentRoomGO.name}");
        }

        // Start dungeon at specific room index
        public void StartAt(int index)
        {
            if (RoomPrefabs == null || index < 0 || index >= RoomPrefabs.Length) return;

            // unload anything existing
            if (_currentRoomGO != null) { _currentRoom.Deactivate(); Destroy(_currentRoomGO); _currentRoomGO = null; _currentRoom = null; }

            _currentIndex = index;
            var prefab = RoomPrefabs[_currentIndex];
            _currentRoomGO = Instantiate(prefab, RoomsParent);
            _currentRoomGO.name = $"{prefab.name}_{_currentIndex}";
            _currentRoom = _currentRoomGO.GetComponent<Room>() ?? _currentRoomGO.AddComponent<Room>();
            _currentRoom.Activate();
        }
    }
}
