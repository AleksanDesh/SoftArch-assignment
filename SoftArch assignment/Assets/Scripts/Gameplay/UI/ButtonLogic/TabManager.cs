using UnityEngine;

namespace DungeonCrawler.UI
{
    public class TabManager : MonoBehaviour
    {
        public void InvertGameObjectEnabling(GameObject go)
        {
            go.SetActive(!go.activeSelf);
        }
    }
}