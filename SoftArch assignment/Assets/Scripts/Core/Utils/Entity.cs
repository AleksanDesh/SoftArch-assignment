using UnityEngine;

namespace DungeonCrawler.Core.Utils
{
    [DisallowMultipleComponent]
    public class Entity : MonoBehaviour
    {
        static int s_nextId = 1;

        [Tooltip("Optional: you can set this manually for predictable IDs (use 0 for auto).")]
        [SerializeField] int _id = 0;
        public int Id => _id;

        [Tooltip("Optional tag for higher level lookup (not the same as GameObject.tag).")]
        public string EntityTag;

        void Awake()
        {
            if (_id == 0)
            {
                _id = s_nextId++;
            }
        }

        void OnEnable()
        {
            EntityManager.Instance?.Register(this);
        }

        void OnDisable()
        {
            EntityManager.Instance?.Unregister(this);
        }

        void OnDestroy()
        {
            EntityManager.Instance?.Unregister(this);
        }
    }
}
