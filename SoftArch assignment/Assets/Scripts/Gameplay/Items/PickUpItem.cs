using DungeonCrawler.Gameplay.Items.Data;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Items
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        /// <summary>
        /// Instantiates the prefab (or adds PickupItem if missing) and initializes it.
        /// Caller must pass a prefab (usually ItemDefinition.WorldPrefab or a generic pickup prefab).
        /// Returns the initialized PickupItem instance.
        /// </summary>
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

            pickup.InitializeInternal(def, qty);

            return pickup;
        }

        void InitializeInternal(ItemDefinition def, int qty)
        {
            item = def;
            quantity = Mathf.Max(0, qty);
            // apply visuals or other setup here (icon, mesh, name, etc.)
            //SyncVisuals();
        }

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
            Destroy(this.gameObject);
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
