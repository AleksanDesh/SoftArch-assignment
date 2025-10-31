using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Core.Utils
{
    [DefaultExecutionOrder(-1000)]
    public class EntityManager : MonoBehaviour
    {
        public static EntityManager Instance { get; private set; }

        readonly Dictionary<int, Entity> _byId = new Dictionary<int, Entity>(1024);

        // Optional mapping for tag-based lookups if needed
        readonly Dictionary<string, List<Entity>> _byTag = new Dictionary<string, List<Entity>>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple EntityManager instances found. Destroying duplicate.");
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _byId.Clear();
            _byTag.Clear();
        }

        public void Register(Entity ent)
        {
            if (ent == null) return;
            if (!_byId.ContainsKey(ent.Id))
            {
                _byId.Add(ent.Id, ent);
            }
            else
            {
                // In case of id collision, overwrite and warn
                _byId[ent.Id] = ent;
                Debug.LogWarning($"EntityManager: duplicate id registered: {ent.Id} (object: {ent.name}). Overwriting.");
            }

            if (!string.IsNullOrEmpty(ent.EntityTag))
            {
                if (!_byTag.TryGetValue(ent.EntityTag, out var list))
                {
                    list = new List<Entity>();
                    _byTag[ent.EntityTag] = list;
                }
                if (!list.Contains(ent)) list.Add(ent);
            }
        }

        public void Unregister(Entity ent)
        {
            if (ent == null) return;
            _byId.Remove(ent.Id);

            if (!string.IsNullOrEmpty(ent.EntityTag) && _byTag.TryGetValue(ent.EntityTag, out var list))
            {
                list.Remove(ent);
                if (list.Count == 0) _byTag.Remove(ent.EntityTag);
            }
        }

        public bool TryGetById(int id, out Entity ent)
        {
            return _byId.TryGetValue(id, out ent);
        }

        public Entity GetById(int id)
        {
            _byId.TryGetValue(id, out var ent);
            return ent;
        }

        public IReadOnlyCollection<Entity> GetAllEntities()
        {
            return _byId.Values;
        }

        public IReadOnlyList<Entity> GetEntitiesWithTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return System.Array.Empty<Entity>();
            if (_byTag.TryGetValue(tag, out var list)) return list;
            return System.Array.Empty<Entity>();
        }

        /// <summary>
        /// Finds the closest entity to position that matches optional tag.
        /// Brute force for now, for better performance, implement spatial partitioning,
        /// or Physics.OverlapSphere with a LayerMask,
        /// if line of sight is needed, add a raycast later on.
        /// </summary>
        public Entity GetClosest(Vector3 position, string tag = null)
        {
            Entity best = null;
            float bestDistSqr = float.MaxValue;

            if (string.IsNullOrEmpty(tag))
            {
                foreach (var e in _byId.Values)
                {
                    if (e == null) continue;
                    float d = (e.transform.position - position).sqrMagnitude;
                    if (d < bestDistSqr)
                    {
                        bestDistSqr = d;
                        best = e;
                    }
                }
            }
            else
            {
                if (_byTag.TryGetValue(tag, out var list))
                {
                    foreach (var e in list)
                    {
                        if (e == null) continue;
                        float d = (e.transform.position - position).sqrMagnitude;
                        if (d < bestDistSqr)
                        {
                            bestDistSqr = d;
                            best = e;
                        }
                    }
                }
            }

            return best;
        }
    }
}
