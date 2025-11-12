using Mirror;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    [DisallowMultipleComponent]
    public class DoorAnimator : NetworkBehaviour
    {
        //public string OpenTrigger = "Open";
        //public string CloseTrigger = "Close";

        //NetworkIdentity parentIdentity;

        //void Awake()
        //{
        //    // prefer parent identity (room root might hold it)
        //    parentIdentity = GetComponentInParent<NetworkIdentity>();
        //}

        //[Server]
        //public void OpenDoor(GameObject door)
        //{
        //    if (door == null) return;

        //    // apply locally first so host sees immediate result
        //    OpenDoorLocal(door);

        //    if (!isServer) return;

        //    // ensure identity
        //    if (parentIdentity == null)
        //        parentIdentity = GetComponentInParent<NetworkIdentity>();

        //    if (parentIdentity == null)
        //    {
        //        Debug.LogWarning($"{name}: No NetworkIdentity found — cannot sync door '{door.name}'. Applying locally only.");
        //        return;
        //    }

        //    string doorPath = GetRelativePath(door.transform, parentIdentity.transform);
        //    Debug.Log($"{name} Server: request open door parentNetId={parentIdentity.netId} path='{doorPath}'");

        //    var manager = NetworkManager.singleton as CustomNetworkManager;
        //    if (manager != null)
        //    {
        //        manager.BroadcastObjectState(parentIdentity.netId, doorPath, true);
        //        Debug.Log($"{name} Server: Broadcasted open to manager for netId={parentIdentity.netId}");
        //    }
        //    else
        //    {
        //        Debug.LogWarning($"{name}: CustomNetworkManager not found — broadcast skipped.");
        //    }
        //}

        //[Server]
        //public void CloseDoor(GameObject door)
        //{
        //    if (door == null) return;

        //    CloseDoorLocal(door);

        //    if (!isServer) return;

        //    if (parentIdentity == null)
        //        parentIdentity = GetComponentInParent<NetworkIdentity>();

        //    if (parentIdentity == null)
        //    {
        //        Debug.LogWarning($"{name}: No NetworkIdentity found — cannot sync door '{door.name}'. Applying locally only.");
        //        return;
        //    }

        //    string doorPath = GetRelativePath(door.transform, parentIdentity.transform);
        //    var manager = NetworkManager.singleton as CustomNetworkManager;
        //    if (manager != null)
        //    {
        //        manager.BroadcastObjectState(parentIdentity.netId, doorPath, false);
        //        Debug.Log($"{name} Server: Broadcasted close to manager for netId={parentIdentity.netId}");
        //    }
        //}

        //// LOCAL LOGIC
        //void OpenDoorLocal(GameObject door)
        //{
        //    Debug.Log($"{name} Opening Door locally: {door?.name}");
        //    if (door == null) return;

        //    // If there's an animator, ensure the object is active before triggering.
        //    var animator = door.GetComponent<Animator>();
        //    if (animator != null)
        //    {
        //        if (!door.activeInHierarchy)
        //        {
        //            // activate so Animator can run
        //            door.SetActive(true);
        //        }

        //        animator.ResetTrigger(CloseTrigger);
        //        animator.SetTrigger(OpenTrigger);
        //    }
        //    else
        //    {
        //        SetDoorActiveAndColliders(door, false); // non-animated open = deactivate door
        //    }
        //}

        //void CloseDoorLocal(GameObject door)
        //{
        //    if (door == null) return;
        //    var animator = door.GetComponent<Animator>();
        //    if (animator != null)
        //    {
        //        // Ensure active so animation runs
        //        if (!door.activeInHierarchy) door.SetActive(true);
        //        animator.ResetTrigger(OpenTrigger);
        //        animator.SetTrigger(CloseTrigger);
        //    }
        //    else
        //    {
        //        SetDoorActiveAndColliders(door, true); // non-animated close = activate door
        //    }
        //}

        //void SetDoorActiveAndColliders(GameObject door, bool active)
        //{
        //    door.SetActive(active);
        //    var colliders = door.GetComponentsInChildren<Collider>(includeInactive: true);
        //    foreach (var c in colliders) c.enabled = active;
        //}

        //// Helper: resolve child under parent identity (used only by TryApply path logic)
        //bool TryResolveDoor(uint parentNetId, string relativePath, out GameObject door)
        //{
        //    door = null;
        //    if (!NetworkClient.spawned.TryGetValue(parentNetId, out var identity))
        //    {
        //        Debug.LogWarning($"{name}: TryResolveDoor: parent netId {parentNetId} not found on client.");
        //        return false;
        //    }

        //    var parent = identity.transform;
        //    if (string.IsNullOrEmpty(relativePath))
        //    {
        //        door = parent.gameObject;
        //        return true;
        //    }

        //    var child = parent.Find(relativePath);
        //    if (child == null)
        //    {
        //        Debug.LogWarning($"{name}: TryResolveDoor: could not find child '{relativePath}' under {parent.name}");
        //        return false;
        //    }

        //    door = child.gameObject;
        //    return true;
        //}

        //string GetRelativePath(Transform target, Transform root)
        //{
        //    if (target == root) return "";
        //    string path = target.name;
        //    var current = target.parent;
        //    while (current != null && current != root)
        //    {
        //        path = current.name + "/" + path;
        //        current = current.parent;
        //    }
        //    return path;
        //}
    }
}
