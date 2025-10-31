using UnityEngine;
using System.Collections.Generic;

namespace DungeonCrawler.Tools
{
    [ExecuteInEditMode]
    public class RandomRotationTool : MonoBehaviour
    {
#if UNITY_EDITOR
        // non-serialized so duplication will have it default to false
        private bool initialized = false;

        // track scheduled instances to avoid scheduling multiple callbacks / leaks
        private static readonly HashSet<int> s_scheduledInstanceIds = new HashSet<int>();

        private void OnValidate()
        {
            // don't run in play mode or while editor is compiling/updating
            if (Application.isPlaying) return;
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return;

            // don't operate on prefab *assets* in the Project window
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;

            int id = GetInstanceID();

            // avoid scheduling more than once for this instance
            if (s_scheduledInstanceIds.Contains(id)) return;
            s_scheduledInstanceIds.Add(id);

            // capture instance and id for safety
            var instance = this;
            UnityEditor.EditorApplication.CallbackFunction delayed = null;
            delayed = () =>
            {
                // always unsubscribe and remove from scheduled set immediately
                UnityEditor.EditorApplication.delayCall -= delayed;
                s_scheduledInstanceIds.Remove(id);

                // object may have been destroyed before this runs
                if (instance == null) return;

                // if somehow became part of a prefab asset, bail
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(instance.gameObject)) return;

                // double-check transform still valid
                if (instance.transform == null) return;

                // only apply once per instance
                if (!instance.initialized)
                {
                    instance.ApplyRandomRotation();
                    instance.initialized = true;

                    // mark dirty so scene knows it's changed
                    UnityEditor.EditorUtility.SetDirty(instance.transform);
                }
            };

            UnityEditor.EditorApplication.delayCall += delayed;
        }

        private void ApplyRandomRotation()
        {
            ApplyRandomRotationToTransform(transform);
        }

        private void ApplyRandomRotationToTransform(Transform t)
        {
            if (t == null) return;
            int[] angles = { 0, 90, 180, 270 };
            float y = angles[UnityEngine.Random.Range(0, angles.Length)];
            t.rotation = Quaternion.Euler(0f, y, 0f);
        }
#endif
    }
}
