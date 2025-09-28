// // MapTypes.cs
// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public enum NodeKind { Start, Combat, Elite, Rest, Shop, Event, Treasure, Boss }

// [Serializable]
// public class MapNodeData
// {
//     public int id;
//     public int layer;                // 0..(layers-1)
//     public NodeKind kind;
//     public List<int> children = new List<int>(); // Next-layer node ids
// }

// [Serializable]
// public class MapGraph
// {
//     public int layers;               // total layers (including start and boss)
//     public int startId;
//     public int bossId;
//     public List<MapNodeData> nodes = new List<MapNodeData>();

//     public MapNodeData Get(int id) => nodes.Find(n => n.id == id);
//     public List<MapNodeData> GetLayer(int layerIdx) => nodes.FindAll(n => n.layer == layerIdx);
// }
