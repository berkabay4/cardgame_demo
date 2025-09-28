// // MapGenerator.cs
// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Random = System.Random;

// [CreateAssetMenu(menuName = "Maps/GeneratorConfig")]
// public class MapGeneratorConfig : ScriptableObject
// {
//     [Header("Layout")]
//     [Min(3)] public int totalLayers = 8;          // 0..(L-1), 0 = Start, L-1 = Boss
//     [Range(1,5)] public int minNodesPerLayer = 2; // except Start/Boss layers
//     [Range(2,7)] public int maxNodesPerLayer = 4;
//     [Range(1,2)] public int minEdgesPerNode = 1;
//     [Range(1,3)] public int maxEdgesPerNode = 2;

//     [Header("Links")]
//     [Range(0f,1f)] public float crossLinkChance = 0.2f; // Ek bağlantı denemesi

//     [Header("Kinds (weights)")]
//     public float combatWeight = 5f;
//     public float eliteWeight  = 1f;
//     public float restWeight   = 1.5f;
//     public float shopWeight   = 1.0f;
//     public float eventWeight  = 1.5f;
//     public float treasureWeight=1.0f;

//     public NodeKind RollKind(Random rng)
//     {
//         // Start/Boss hariç düğümler için ağırlıklı seçim
//         var weights = new (NodeKind kind, float w)[] {
//             (NodeKind.Combat, combatWeight),
//             (NodeKind.Elite,  eliteWeight),
//             (NodeKind.Rest,   restWeight),
//             (NodeKind.Shop,   shopWeight),
//             (NodeKind.Event,  eventWeight),
//             (NodeKind.Treasure, treasureWeight),
//         };

//         float sum = 0f; foreach (var x in weights) sum += x.w;
//         float r = (float)(rng.NextDouble() * sum);
//         foreach (var (k,w) in weights)
//         {
//             if (r < w) return k;
//             r -= w;
//         }
//         return NodeKind.Combat;
//     }
// }

// public static class MapGenerator
// {
//     public static MapGraph Generate(MapGeneratorConfig cfg, int seed)
//     {
//         var rng = new Random(seed);
//         var g = new MapGraph { layers = cfg.totalLayers };

//         int idCounter = 0;

//         // Layer 0: Start
//         var start = new MapNodeData { id = idCounter++, layer = 0, kind = NodeKind.Start };
//         g.nodes.Add(start);
//         g.startId = start.id;

//         // Middle layers: 1 .. L-2
//         for (int layer = 1; layer < cfg.totalLayers - 1; layer++)
//         {
//             int count = rng.Next(cfg.minNodesPerLayer, cfg.maxNodesPerLayer + 1);
//             for (int i = 0; i < count; i++)
//             {
//                 g.nodes.Add(new MapNodeData {
//                     id = idCounter++,
//                     layer = layer,
//                     kind = cfg.RollKind(rng)
//                 });
//             }
//         }

//         // Last layer: Boss (single)
//         var boss = new MapNodeData { id = idCounter++, layer = cfg.totalLayers - 1, kind = NodeKind.Boss };
//         g.nodes.Add(boss);
//         g.bossId = boss.id;

//         // Connect layers
//         for (int layer = 0; layer < cfg.totalLayers - 1; layer++)
//         {
//             var current = g.GetLayer(layer);
//             var next    = g.GetLayer(layer + 1);

//             // Emniyet: next layer boşsa bir tane oluştur (olmaz ama koruma)
//             if (next.Count == 0)
//             {
//                 var fallback = new MapNodeData {
//                     id = idCounter++,
//                     layer = layer + 1,
//                     kind = (layer + 1 == cfg.totalLayers -1) ? NodeKind.Boss : cfg.RollKind(rng)
//                 };
//                 g.nodes.Add(fallback);
//                 next = g.GetLayer(layer + 1);
//                 if (fallback.kind == NodeKind.Boss) g.bossId = fallback.id;
//             }

//             // Her current düğümü next’te 1–2 düğüme bağla
//             foreach (var n in current)
//             {
//                 int edges = rng.Next(cfg.minEdgesPerNode, cfg.maxEdgesPerNode + 1);
//                 for (int e = 0; e < edges; e++)
//                 {
//                     var target = next[rng.Next(0, next.Count)];
//                     if (!n.children.Contains(target.id))
//                         n.children.Add(target.id);
//                 }

//                 // Opsiyonel ekstra çapraz bağlantı
//                 if (rng.NextDouble() < cfg.crossLinkChance && next.Count > 1)
//                 {
//                     var target = next[rng.Next(0, next.Count)];
//                     if (!n.children.Contains(target.id))
//                         n.children.Add(target.id);
//                 }
//             }

//             // Reachability emniyeti: next layer’daki her node’un en az 1 parent’ı olsun
//             foreach (var candidate in next)
//             {
//                 bool hasParent = g.nodes.Exists(p => p.layer == layer && p.children.Contains(candidate.id));
//                 if (!hasParent)
//                 {
//                     // bir parent ekle
//                     var parent = current[rng.Next(0, current.Count)];
//                     if (!parent.children.Contains(candidate.id))
//                         parent.children.Add(candidate.id);
//                 }
//             }
//         }

//         // Start’tan Boss’a en az 1 yol olduğundan eminiz (katmanlar ardışık)
//         return g;
//     }
// }
