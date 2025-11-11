using DungeonCrawler.Core.Utils;
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
            Entity ent = other.GetComponentInParent<Entity>();
            if (ent.tag != PlayerTag) return;

            // Notify manager to transition
            dm.RequestRoomEntranceOpen(this);
        }
    }
}
