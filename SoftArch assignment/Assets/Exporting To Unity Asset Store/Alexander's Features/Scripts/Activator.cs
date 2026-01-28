using UnityEngine;
namespace AlexanderFeatures
{
    public class Activator : MonoBehaviour
    {
        public void InvertGameObjectEnabling(GameObject go)
        {
            go.SetActive(!go.activeSelf);
        }

    }
}