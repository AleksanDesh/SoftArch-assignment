using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Put one of these on corridor trigger areas (trigger collider).
    /// When the player enters, it requests a transition to the next room from DungeonManager.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CorridorTrigger : MonoBehaviour
    {
        [Tooltip("Tag of the player GameObject or leave empty to use EntityManager lookup.")]
        public string PlayerTag = "Player";

        [Tooltip("Optional: which door set to close on trigger (useful if the corridor belongs to a specific room)")]
        public Room RoomToAffect;

        DungeonManager dm;

        private void Start()
        {
            dm = Object.FindFirstObjectByType<DungeonManager>();
        }

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }


        // Change this later to something suiting, or leave as is, should work
        void OnTriggerEnter(Collider other)
        {
            // Prefer Entity presence check if you have an Entity component
            var ent = other.GetComponentInParent<Core.Utils.Entity>();
            if (ent == null)
            {
                if (!string.IsNullOrEmpty(PlayerTag) && other.gameObject.CompareTag(PlayerTag))
                {
                    // ok - continue
                }
                else return;
            }

            // Close doors immediately for the current room (if provided)
            if (RoomToAffect != null) RoomToAffect.CloseAllExceptEntrance();

            // Notify manager to transition
            dm.RequestRoomEntranceOpen(this);
        }
    }
}
