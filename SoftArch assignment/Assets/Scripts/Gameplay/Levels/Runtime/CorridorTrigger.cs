using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using Mirror;
using UnityEngine;

namespace DungeonCrawler.Levels.Runtime
{
    /// <summary>
    /// Put one of these on corridor trigger areas (trigger collider).
    /// When the player enters, it requests a transition to the next room from DungeonManager.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CorridorTrigger : NetworkBehaviour
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
        [ServerCallback]
        void OnTriggerEnter(Collider other)
        {
            var ent = other.GetComponentInParent<Entity>();
            if (ent == null || ent.tag != PlayerTag) return;
            this.GetComponent<Collider>().enabled = false;
            //var ni = ent.GetComponent<NetworkIdentity>();
            //if (ni == null || ni.connectionToClient == null) return;
            dm?.RequestRoomEntranceOpen(this);
        }
    }
}
