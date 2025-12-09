using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items.Data;
using Mirror;
using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Entity))]
    public class PickupItem : NetworkBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;

        [Header("Lifetime (auto destroy)")]
        [Tooltip("If true, this pickup will be destroyed after LifetimeSeconds.")]
        [SerializeField] private bool useLifetime = true;
        [Tooltip("Seconds until this pickup is automatically destroyed (if Use Lifetime is true).")]
        [SerializeField] private float lifetimeSeconds = 30f;

        // runtime
        public ItemDefinition Item => item;
        public int Quantity => quantity;

        private Coroutine _lifetimeRoutine;

        public override void OnStopServer()
        {
            CancelLifetimeTimer();
            Remove();
            base.OnStopServer();
        }

        /// <summary>
        /// Instantiates the prefab (or adds PickupItem if missing) and initializes it.
        /// Caller must pass a prefab (usually ItemDefinition.WorldPrefab or a generic pickup prefab).
        /// Returns the initialized PickupItem instance.
        /// </summary>
        [Server]
        public static PickupItem Spawn(GameObject prefab, ItemDefinition def, int qty, Vector3 pos, Quaternion rot)
        {
            if (prefab == null)
            {
                Debug.LogError("[PickupItem.Spawn] prefab is null.");
                return null;
            }

            var go = Object.Instantiate(prefab, pos, rot);
            // ensure a PickupItem component exists
            var pickup = go.GetComponent<PickupItem>();
            if (pickup == null) pickup = go.AddComponent<PickupItem>();
            var entity = go.GetComponent<Entity>();
            if (entity == null) entity = go.AddComponent<Entity>();

            pickup.InitializeInternal(def, qty, entity);
            NetworkServer.Spawn(go);

            return pickup;
        }

        void InitializeInternal(ItemDefinition def, int qty, Entity entity)
        {
            item = def;
            quantity = Mathf.Max(0, qty);
            entity.EntityTag = def.ItemId;
            //Debug.Log($"Spawning item with expected name {def.ItemId}, and current entity is {entity.EntityTag}");

            // apply visuals or other setup here (icon, mesh, name, etc.)
            // SyncVisuals();

            // start the lifetime timer if enabled
            if (useLifetime && lifetimeSeconds > 0f)
            {
                StartLifetimeTimer(lifetimeSeconds);
            }
        }

        [Server]
        public int TryTake(int amount)
        {
            if (amount <= 0) return 0;
            int taken = Mathf.Min(amount, Quantity);
            quantity -= taken;

            if (Quantity <= 0)
                OnEmpty();

            return taken;
        }

        private void OnEmpty()
        {
            // cancel any pending auto-destroy coroutine to prevent extra logs/work
            CancelLifetimeTimer();
            Remove();
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        // -----------------------
        // Lifetime timer helpers
        // -----------------------

        /// <summary>
        /// Starts or restarts the lifetime timer. When the timer ends the GameObject is destroyed.
        /// </summary>
        /// <param name="seconds">Seconds until destruction.</param>
        void StartLifetimeTimer(float seconds)
        {
            CancelLifetimeTimer();
            _lifetimeRoutine = StartCoroutine(LifetimeCoroutine(seconds));
        }

        /// <summary>
        /// Cancels the lifetime timer if running.
        /// </summary>
        void CancelLifetimeTimer()
        {
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
                _lifetimeRoutine = null;
            }
        }

        private IEnumerator LifetimeCoroutine(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // final check in case something changed (e.g., quantity reached 0 and OnEmpty was called)
            if (this != null)
            {
                Remove();
            }
        }

        private void Remove()
        {
            NetworkServer.Destroy(this.gameObject);
        }
    }
}
