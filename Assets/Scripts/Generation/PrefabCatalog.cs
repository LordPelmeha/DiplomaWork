using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diploma.Generation
{
    [CreateAssetMenu(menuName = "Diploma/Prefab Catalog", fileName = "PrefabCatalog")]
    public sealed class PrefabCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int id;
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<int, GameObject> _map;

        private void OnEnable()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            _map = new Dictionary<int, GameObject>(entries.Count);
            foreach (var e in entries)
            {
                if (e.prefab == null) continue;
                _map[e.id] = e.prefab;
            }
        }

        public bool TryGetPrefab(int id, out GameObject prefab)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(id, out prefab);
        }
    }
}