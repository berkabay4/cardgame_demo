// // MapProgress.cs
// using System.Collections.Generic;
// using UnityEngine;

// [DisallowMultipleComponent]
// public class MapProgress : MonoBehaviour
// {
//     public static MapProgress Instance { get; private set; }

//     [Header("Options")]
//     [SerializeField] private bool freeJumpToAnyNodeInNextLayer = false;

//     public MapGraph Graph { get; private set; }
//     public int CurrentNodeId { get; private set; }
//     public HashSet<int> Visited = new HashSet<int>();
//     public HashSet<int> Unlocked = new HashSet<int>(); // UI için

//     void Awake()
//     {
//         if (Instance && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     public void LoadGraph(MapGraph graph)
//     {
//         Graph = graph;
//         Visited.Clear();
//         Unlocked.Clear();

//         CurrentNodeId = graph.startId;
//         Visited.Add(CurrentNodeId);
//         UnlockNextLayerFrom(CurrentNodeId);
//     }

//     public List<MapNodeData> GetNextOptions()
//     {
//         var cur = Graph.Get(CurrentNodeId);
//         var nextLayer = cur.layer + 1;
//         var options = new List<MapNodeData>();

//         if (freeJumpToAnyNodeInNextLayer)
//         {
//             options = Graph.GetLayer(nextLayer);
//         }
//         else
//         {
//             foreach (var childId in cur.children)
//                 options.Add(Graph.Get(childId));
//         }
//         return options;
//     }

//     public bool CanMoveTo(int targetId)
//     {
//         var target = Graph.Get(targetId);
//         if (target == null) return false;

//         if (freeJumpToAnyNodeInNextLayer)
//             return target.layer == Graph.Get(CurrentNodeId).layer + 1;
//         else
//             return Graph.Get(CurrentNodeId).children.Contains(targetId);
//     }

//     public void CompleteCurrentNodeAndMoveTo(int targetId)
//     {
//         if (!CanMoveTo(targetId))
//         {
//             Debug.LogWarning($"Invalid move to {targetId}");
//             return;
//         }

//         CurrentNodeId = targetId;
//         Visited.Add(CurrentNodeId);
//         Unlocked.Clear();
//         UnlockNextLayerFrom(CurrentNodeId);

//         // Boss’a ulaşıldı mı?
//         if (CurrentNodeId == Graph.bossId)
//         {
//             Debug.Log("Reached BOSS!");
//             // Boss dövüşü başlat vs.
//         }
//     }

//     private void UnlockNextLayerFrom(int nodeId)
//     {
//         var cur = Graph.Get(nodeId);
//         var nextLayer = cur.layer + 1;
//         if (nextLayer >= Graph.layers) return;

//         if (freeJumpToAnyNodeInNextLayer)
//         {
//             foreach (var n in Graph.GetLayer(nextLayer))
//                 Unlocked.Add(n.id);
//         }
//         else
//         {
//             foreach (var id in cur.children)
//                 Unlocked.Add(id);
//         }
//     }
// }
