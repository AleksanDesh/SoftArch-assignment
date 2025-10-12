using UnityEngine;

namespace DungeonCrawler.Tools
{
    [ExecuteInEditMode]
    public class RandomRotationTool : MonoBehaviour
    {
#if UNITY_EDITOR
        private bool initialized = false;

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // Delay to handle duplication correctly
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (!initialized)
                {
                    ApplyRandomRotation();
                    initialized = true;
                    UnityEditor.EditorUtility.SetDirty(transform);
                }
            };
        }

        private void ApplyRandomRotation()
        {
            int[] angles = { 0, 90, 180, 270 };
            float y = angles[Random.Range(0, angles.Length)];
            transform.rotation = Quaternion.Euler(0, y, 0);
        }
#endif
    }
}
