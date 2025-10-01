// using UnityEngine;

// public class MapBridge : MonoBehaviour
// {
//     void OnEnable() {
//         // Eğer senin MapPlayerTracker SelectNode ile gidiyorsa,
//         // SelectNode içinde GameFlowDirector'a gönderebilirsin.
//         // Burada gösterim için public metod bırakalım.
//     }

//     public void OnNodeSelected(Map.MapNode node)
//     {
//         if (node == null) return;
//         if (node.Node.nodeType == Map.NodeType.MinorEnemy)
//         {
//             GameFlowDirector.Instance.StartMinorEncounter(node);
//         }
//         else
//         {
//             // Diğer tipler: Rest/Shop/Event vs.
//             Debug.Log($"[MapBridge] Non-combat node selected: {node.Node.nodeType}");
//         }
//     }
// }
