using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    [Serializable]
    public struct NodeTypeSkin {
        public NodeType type;
        public NodeBlueprint blueprint;
    }

    public class NodeSkinSet : MonoBehaviour
    {
        [Header("NodeType → Blueprint eşlemeleri")]
        public List<NodeTypeSkin> skins = new();

        [Header("Fallback (boş yakalanırsa kullanılacak)")]
        public NodeBlueprint defaultBlueprint; // inspector'dan basit bir ikon ver

        private Dictionary<NodeType, NodeBlueprint> cache;

        void Awake() {
            cache = new Dictionary<NodeType, NodeBlueprint>();
            foreach (var s in skins) {
                if (s.blueprint == null) continue;
                cache[s.type] = s.blueprint;
            }

            // Log: hangi tipler atanmış
            Debug.Log($"[NodeSkinSet] Cached {cache.Count} entries. Default={(defaultBlueprint ? defaultBlueprint.name : "NULL")}");
        }

        public NodeBlueprint Resolve(Node node) {
            if (node == null) {
                Debug.LogWarning("[NodeSkinSet] Resolve(node=null) → default");
                return defaultBlueprint;
            }

            if (cache != null && cache.TryGetValue(node.nodeType, out var bp) && bp != null)
                return bp;

            Debug.LogWarning($"[NodeSkinSet] Missing blueprint for NodeType={node.nodeType}. Using default.");
            return defaultBlueprint;
        }

#if UNITY_EDITOR
        void OnValidate() {
            // Hangi NodeType'lar eksik? (editörde yardımcı uyarı)
            var seen = new HashSet<NodeType>();
            foreach (var s in skins) if (s.blueprint) seen.Add(s.type);

            var missing = new List<string>();
            foreach (NodeType t in Enum.GetValues(typeof(NodeType)))
                if (!seen.Contains(t)) missing.Add(t.ToString());

            if (missing.Count > 0) {
                Debug.LogWarning($"[NodeSkinSet] Eksik blueprint: {string.Join(", ", missing)}");
            }
        }
#endif
    }
}
