using DungeonCrawler.Gameplay.Inventory.Model;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonCrawler.Gameplay.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public GameObject InventorySlot;

        Model.Inventory _inventory;
        Transform _slotParent;
        Dictionary<int, Transform> _indexReference = new Dictionary<int, Transform>();
        bool _settedUp = false;
        private void OnEnable()
        {
            Setup();
        }
        void Setup()
        {
            if (_settedUp) return;
            if (Systems.GameManager.Instance.localPlayer != null)
                _inventory = Systems.GameManager.Instance.localPlayer.GetComponent<Model.Inventory>();
            else
            {
                Debug.LogWarning("No local player found, have you started the server and spawned?");
                return;
            }
            Debug.Log("I run");

                _inventory.OnSlotChanged += UpdateSlot;
            if (_inventory == null) return;
            if (InventorySlot == null) { Debug.LogWarning($"{this.gameObject.name} requires InventorySlot prefab to be set to function"); return; }
            _slotParent = this.transform.Find("Items parent");

            for (int i = 0; i < _inventory.Capacity; i++)
            {
                GameObject go = Instantiate(InventorySlot, _slotParent);
                var slot = go.GetComponent<InventorySlotUI>();
                slot.SetIndex(i);
                slot.OnUsed += HandleFromSlot;
                _indexReference.Add(i, go.transform);
                UpdateSlot(i);
            }
            _settedUp = true;
        }


        void HandleFromSlot(int idx)
        {
            _inventory.Use(idx);
        }

        void UpdateSlot(int idx)
        {
            if (_inventory.Slots[idx].Quantity > 0)
            {
                ActiveSlotSetUp(idx);
            }
            else
            {
                DeactiveSlotSetUp(idx);
            }
        }

        void ActiveSlotSetUp(int idx)
        {
            Transform uiSlot = _indexReference[idx];
            InventorySlot slotData = _inventory.Slots[idx];
            Image uiIcon = uiSlot.Find("Item button/Icon").GetComponent<Image>();
            uiIcon.sprite = slotData.Definition.Icon;
            uiIcon.enabled = true;
            TextMeshProUGUI uiQuantity = uiSlot.Find("Item button/Amount").GetComponent<TextMeshProUGUI>();
            uiQuantity.text = slotData.Quantity.ToString();
            Button uiBtn = uiSlot.Find("Delete button").GetComponent<Button>();
            uiBtn.interactable = true;
        }

        void DeactiveSlotSetUp(int idx)
        {
            Transform uiSlot = _indexReference[idx];
            Image uiIcon = uiSlot.Find("Item button/Icon").GetComponent<Image>();
            uiIcon.sprite = null;
            uiIcon.enabled = false;
            TextMeshProUGUI uiQuantity = uiSlot.Find("Item button/Amount").GetComponent<TextMeshProUGUI>();
            uiQuantity.text = "";
            Button uiBtn = uiSlot.Find("Delete button").GetComponent<Button>();
            uiBtn.interactable = false;
        }
    }
}