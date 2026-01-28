using System;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Inventory
{
    public class InventorySlotUI : MonoBehaviour
    {
        int _index;
        public event Action<int> OnUsed;
        public void SetIndex(int idx)
        {
            _index = idx;
        }

        public void UseItem()
        {
            OnUsed?.Invoke(_index);
        }
    }
}