using UnityEngine;

namespace AlexanderFeatures
{
    [DisallowMultipleComponent]
    public class Identity : MonoBehaviour
    {
        static int s_nextId = 1;

        [Tooltip("Optional: you can set this manually for predictable IDs (use 0 for auto).")]
        [SerializeField] private int _id = 0;
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
    }
}