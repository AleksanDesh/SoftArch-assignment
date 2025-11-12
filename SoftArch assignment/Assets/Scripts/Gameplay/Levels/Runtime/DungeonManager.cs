using Mirror;
using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    public class DungeonManager : NetworkBehaviour
    {
        [Tooltip("Parent containing all rooms and corridors in order (Room, Corridor, Room, Corridor, ...)")]
        public Transform RoomsParent;

        [Tooltip("Delay between transitions")]
        public float TransitionDelay = 0.8f;

        GameObject[] sequence;
        int _currentIndex = -1;

        Room _currentRoom;
        GameObject _currentRoomGO;
        GameObject _currentCorridorGO;
        Room _pendingRoom;
        GameObject _pendingRoomGO;

        void Awake()
        {
            if (RoomsParent == null)
            {
                Debug.LogError("DungeonManager: RoomsParent not assigned!");
                return;
            }

            // Build ordered list from hierarchy
            int count = RoomsParent.childCount;
            sequence = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                sequence[i] = RoomsParent.GetChild(i).gameObject;
            }

            //Debug.Log($"DungeonManager: Registered {sequence.Length} dungeon parts.");
        }

        [Server]
        public void StartAt(int index = 0)
        {
            // Ensure everything initially disabled server-side (and notify any connected clients)
            for (int i = 0; i < sequence.Length; i++)
            {
                //Debug.Log($"Despawning sequence {i}");
                if (sequence[i] != null) SetActiveNetworkedByIndex(i, false);
            }

            if (index < 0 || index >= sequence.Length)
            {
                Debug.LogWarning("DungeonManager: Invalid start index.");
                return;
            }

            _currentIndex = index;

            AssignRoomFromChild(_currentIndex, out _currentRoomGO, out _currentRoom);

            // Server sets its own state and notifies clients by index
            SetActiveNetworkedByIndex(_currentIndex, true);

            _currentRoom?.Activate();
            _currentRoom?.OpenEntrance();

            //Debug.Log($"DungeonManager: Started dungeon at {_currentRoomGO?.name}");
        }

        [Server]
        public void RequestRoomEntranceOpen(CorridorTrigger corridor)
        {
            if (_pendingRoom != null)
            {
                //Debug.Log($"DungeonManager: Opening entrance for next room {_pendingRoom.name}");
                _pendingRoom.OpenEntrance();
            }
        }

        [Server]
        public void NotifyRoomCleared(Room clearedRoom)
        {
            if (clearedRoom == null || clearedRoom != _currentRoom)
                return;

            int corridorIndex = _currentIndex + 1;
            int nextRoomIndex = _currentIndex + 2;

            if (nextRoomIndex >= sequence.Length)
            {
                Debug.Log("DungeonManager: Dungeon completed! No more rooms.");
                return;
            }

            // Enable corridor
            if (corridorIndex < sequence.Length)
            {
                _currentCorridorGO = sequence[corridorIndex];
                if (_currentCorridorGO != null)
                {
                    SetActiveNetworkedByIndex(corridorIndex, true);
                    Debug.Log($"DungeonManager: Enabled corridor {_currentCorridorGO.name}");
                }
            }

            // Enable next room (pending)
            AssignRoomFromChild(nextRoomIndex, out _pendingRoomGO, out _pendingRoom);
            if (_pendingRoomGO != null)
            {
                SetActiveNetworkedByIndex(nextRoomIndex, true);
                _pendingRoom?.Activate(); // spawn enemies on server
            }
        }

        [Server]
        public void NotifyPlayersInside(Room newRoom)
        {
            if (newRoom == null || newRoom != _pendingRoom)
                return;

            //Debug.Log($"DungeonManager: Players entered room {newRoom.name}");

            // Disable previous room & corridor (networked by index)
            if (_currentRoomGO != null)
            {
                _currentRoom?.Deactivate();
                int prevIndex = _currentIndex;
                if (prevIndex >= 0) SetActiveNetworkedByIndex(prevIndex, false);
            }

            if (_currentCorridorGO != null)
            {
                int corridorIndex = _currentIndex + 1;
                if (corridorIndex < sequence.Length) SetActiveNetworkedByIndex(corridorIndex, false);
            }

            // Promote new room
            _currentRoomGO = _pendingRoomGO;
            _currentRoom = _pendingRoom;
            _currentIndex += 2;

            _pendingRoomGO = null;
            _pendingRoom = null;
        }

        void AssignRoomFromChild(int index, out GameObject roomGO, out Room room)
        {
            roomGO = sequence[index];
            room = null;
            if (roomGO == null) return;

            // Prefer NetworkIdentity present in scene; warn if missing
            var ni = roomGO.GetComponent<NetworkIdentity>();
            if (ni == null)
            {
                Debug.LogWarning($"DungeonManager: GameObject '{roomGO.name}' has NO NetworkIdentity. That's okay if it's a pure scene object, but ensure it's present identically on clients.");
            }

            room = roomGO.GetComponent<Room>();
            if (room == null)
                room = roomGO.AddComponent<Room>();
        }

        #region NetworkResolving

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (sequence == null || sequence.Length == 0)
            {
                Debug.LogWarning("DungeonManager: No sequence found!");
                return;
            }

            //Debug.Log($"{name}: Dungeon Manager is starting on the server");
            StartCoroutine(ActivateNextFrame());
        }


        IEnumerator ActivateNextFrame()
        {
            yield return new WaitForEndOfFrame();

            StartAt();
        }


        // Server helper: set active by index, set on server and notify clients
        [Server]
        void SetActiveNetworkedByIndex(int index, bool state)
        {
            if (index < 0 || index >= sequence.Length) return;

            var go = sequence[index];
            if (go == null) return;

            // Set on server first
            go.SetActive(state);

            // Tell all clients to set the corresponding index active/inactive
            RpcSetActiveByIndex(index, state);
        }

        // ClientRpc: active change for connected clients
        [ClientRpc]
        void RpcSetActiveByIndex(int index, bool state)
        {
            // Runs on clients
            if (sequence == null) return;
            if (index < 0 || index >= sequence.Length)
            {
                Debug.LogWarning($"RpcSetActiveByIndex: invalid index {index}");
                return;
            }

            var go = sequence[index];
            if (go == null)
            {
                Debug.LogWarning($"RpcSetActiveByIndex: sequence[{index}] is null on client.");
                return;
            }

            go.SetActive(state);
        }

        // TargetRpc: send full current state to a single client (used for late joiners)
        // Called from server with the client's connection.
        [TargetRpc]
        public void TargetSendFullState(NetworkConnection target, bool[] states)
        {
            // This runs on the target client: apply states array to local sequence
            if (states == null) return;
            if (sequence == null) return;

            int len = Mathf.Min(states.Length, sequence.Length);
            for (int i = 0; i < len; i++)
            {
                var go = sequence[i];
                if (go != null) go.SetActive(states[i]);
            }
        }

        // Helper server-side to build state array and call TargetSendFullState
        [Server]
        public void SendFullStateToClient(NetworkConnectionToClient conn)
        {
            if (conn == null) return;
            if (sequence == null) return;

            bool[] states = new bool[sequence.Length];
            for (int i = 0; i < sequence.Length; i++)
                states[i] = sequence[i] != null && sequence[i].activeSelf;

            // calls TargetSendFullState on that client
            TargetSendFullState(conn, states);
        }

        #endregion
    }
}
