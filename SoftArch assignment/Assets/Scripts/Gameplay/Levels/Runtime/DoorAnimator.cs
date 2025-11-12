using UnityEngine;
using Mirror;

namespace DungeonCrawler.Levels.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]

    public class DoorAnimator : NetworkBehaviour
    {
        [Tooltip("Animator trigger name used to open an animated door.")]
        public string OpenTrigger = "Open";
        [Tooltip("Animator trigger name used to close an animated door.")]
        public string CloseTrigger = "Close";

        
        NetworkIdentity parentIdentity;

        void Awake()
        {
            parentIdentity = GetComponent<NetworkIdentity>();
        }

        [Server]
        public void OpenDoor(GameObject door)
        {
            if (door == null) return;

            if (isServer)
            {

                if (parentIdentity == null)
                {
                    Debug.LogWarning($"{name}: No NetworkIdentity found in parents — cannot sync door '{door.name}'.");
                    OpenDoorLocal(door);
                    return;
                }

                // compute relative path of the door within its room
                string doorPath = GetRelativePath(door.transform, parentIdentity.transform);
                Debug.Log($"{this.name} Server opens door with {parentIdentity.netId} and the door path {doorPath}");
                RpcOpenDoor(parentIdentity.netId, doorPath);

                // apply locally for host
                OpenDoorLocal(door);
            }
            else
            {
                OpenDoorLocal(door);
            }
        }

        [Server]
        public void CloseDoor(GameObject door)
        {
            if (door == null) return;

            if (isServer)
            {
                if (parentIdentity == null)
                {
                    Debug.LogWarning($"{name}: No NetworkIdentity found— cannot sync door '{door.name}'.");
                    CloseDoorLocal(door);
                    return;
                }

                string doorPath = GetRelativePath(door.transform, parentIdentity.transform);
                RpcCloseDoor(parentIdentity.netId, doorPath);

                CloseDoorLocal(door);
            }
            else
            {
                CloseDoorLocal(door);
            }
        }

        #region LocalLogic

        void OpenDoorLocal(GameObject door)
        {
            Debug.Log($"{this.name} Opening Door locally");
            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger(CloseTrigger);
                animator.SetTrigger(OpenTrigger);
            }
            else
            {
                SetDoorActiveAndColliders(door, false);
            }
        }

        void CloseDoorLocal(GameObject door)
        {
            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger(OpenTrigger);
                animator.SetTrigger(CloseTrigger);
            }
            else
            {
                SetDoorActiveAndColliders(door, true);
            }
        }

        void SetDoorActiveAndColliders(GameObject door, bool active)
        {
            door.SetActive(active);
            var colliders = door.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in colliders) c.enabled = active;
        }
        #endregion
        #region RPC

        [ClientRpc]
        void RpcOpenDoor(uint roomNetId, string doorPath)
        {
            Debug.Log($"{this.name} Received open door");
            if (TryResolveDoor(roomNetId, doorPath, out var door))
                OpenDoorLocal(door);
        }

        [ClientRpc]
        void RpcCloseDoor(uint roomNetId, string doorPath)
        {
            if (TryResolveDoor(roomNetId, doorPath, out var door))
                CloseDoorLocal(door);
        }
        #endregion
        #region Helpers

        bool TryResolveDoor(uint parentNetId, string relativePath, out GameObject door)
        {
            door = null;
            if (!NetworkClient.spawned.TryGetValue(parentNetId, out var identity)) return false;
            var parent = identity.transform;
            var child = parent.Find(relativePath);
            if (child == null)
            {
                Debug.LogWarning($"DoorAnimator: could not find child '{relativePath}' under {parent.name}");
                return false;
            }

            door = child.gameObject;
            return true;
        }

        string GetRelativePath(Transform target, Transform root)
        {
            string path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
        #endregion
    }
}
