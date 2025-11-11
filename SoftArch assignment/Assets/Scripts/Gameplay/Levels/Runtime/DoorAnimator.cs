using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Door abstraction that either calls Animator triggers (if Animator present on door)
    /// or toggles the door GameObject (where the GameObject represents the closed state).
    ///
    /// Semantics:
    /// - OpenDoor(...)  -> attempts to play "open" on Animator; otherwise disables the door GameObject
    /// - CloseDoor(...) -> attempts to play "close" on Animator; otherwise enables the door GameObject
    /// Also toggles colliders when not using Animator.
    /// </summary>
    [DisallowMultipleComponent]
    public class DoorAnimator : MonoBehaviour
    {
        [Tooltip("Animator trigger name used to open an animated door.")]
        public string OpenTrigger = "Open";
        [Tooltip("Animator trigger name used to close an animated door.")]
        public string CloseTrigger = "Close";

        /// <summary>Open the door (make passage available).</summary>
        public void OpenDoor(GameObject door)
        {
            if (door == null) return;

            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                // Try to play animation
                animator.ResetTrigger(CloseTrigger);
                animator.SetTrigger(OpenTrigger);
            }
            else
            {
                // Treat the door GameObject as "closed visual/collider".
                // When open, disable it so passage is clear.
                SetDoorActiveAndColliders(door, false);
            }
        }

        /// <summary>Close the door (block passage).</summary>
        public void CloseDoor(GameObject door)
        {
            if (door == null) return;

            var animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger(OpenTrigger);
                animator.SetTrigger(CloseTrigger);
            }
            else
            {
                // When closed, enable visual/collider.
                SetDoorActiveAndColliders(door, true);
            }
        }

        void SetDoorActiveAndColliders(GameObject door, bool active)
        {
            // set active on the root door object (if the design expects children to be part of door, you may prefer enabling colliders only)
            door.SetActive(active);

            // Also ensure colliders are enabled/disabled to match active state
            var colliders = door.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in colliders) c.enabled = active;
        }
    }
}
