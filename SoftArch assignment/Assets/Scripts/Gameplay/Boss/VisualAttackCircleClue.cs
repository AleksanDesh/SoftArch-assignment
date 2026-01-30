using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Boss
{
    [DisallowMultipleComponent]
    public class VisualAttackCircleClue : MonoBehaviour
    {
        const float RaycastHeight = 10f;   // how high above sample points to start the raycast
        const float SurfaceOffset = 0.02f; // small offset above ground to avoid z-fighting

        LineRenderer _lr;

        /// <summary>
        /// Static helper to create and show a visual attack circle clue on the client.
        /// </summary>
        public static void Show(Vector3 center, float radius, float duration, int segments = 32)
        {
            // create GameObject container
            GameObject go = new GameObject("VisualAttackCircleClue");
            var tele = go.AddComponent<VisualAttackCircleClue>();
            tele.Setup(center, radius, duration, Mathf.Max(8, segments));
        }

        void Setup(Vector3 center, float radius, float duration, int segments)
        {
            // Add and configure LineRenderer
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.loop = true;
            _lr.positionCount = segments;
            _lr.useWorldSpace = true;
            _lr.widthCurve = AnimationCurve.Constant(0f, 1f, 0.1f);
            _lr.startWidth = 0.5f;
            _lr.endWidth = 0.5f;
            _lr.numCornerVertices = 4;
            _lr.numCapVertices = 4;

            // Single-color unlit material
            var mat = new Material(Shader.Find("Sprites/Default"));
            _lr.material = mat;
            _lr.startColor = Color.red;
            _lr.endColor = Color.red;

            int groundMask = BuildGroundRaycastMask();

            // sample the ground and set points
            Vector3[] pts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 sampleTop = center + dir * radius + Vector3.up * RaycastHeight;

                // cast down to find surface point
                if (Physics.Raycast(sampleTop, Vector3.down, out RaycastHit hit, RaycastHeight * 2f, groundMask))
                {
                    pts[i] = hit.point + hit.normal * SurfaceOffset;
                }
                else
                {
                    // fallback: place on center's Y (in case of no hit)
                    pts[i] = center + dir * radius;
                }
            }

            _lr.SetPositions(pts);

            // keep root at origin (positions are world-space)
            gameObject.transform.position = Vector3.zero;

            // destroy after duration
            Destroy(gameObject, duration);
        }

        static int BuildGroundRaycastMask()
        {
            int mask = Physics.DefaultRaycastLayers;

            int bossLayer = LayerMask.NameToLayer("Boss");
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            if (bossLayer >= 0) mask &= ~(1 << bossLayer);
            if (playerLayer >= 0) mask &= ~(1 << playerLayer);
            if (enemyLayer >= 0) mask &= ~(1 << enemyLayer);

            return mask;
        }
    }
}
