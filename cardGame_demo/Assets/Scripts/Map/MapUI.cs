// // MapUI.cs
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class MapUI : MonoBehaviour
// {
//     [Header("Refs")]
//     [SerializeField] private RectTransform canvasArea;
//     [SerializeField] private GameObject nodeButtonPrefab; // Üzerinde Button + TMP_Text olsun
//     [SerializeField] private Color lockedColor = new Color(0.5f,0.5f,0.5f,0.6f);
//     [SerializeField] private Color unlockedColor = Color.white;
//     [SerializeField] private Color visitedColor  = new Color(0.7f,1f,0.7f,1f);

//     [Header("Layout")]
//     [SerializeField] private float padding = 80f;

//     private MapProgress P => MapProgress.Instance;
//     private readonly Dictionary<int, GameObject> _nodeViews = new();

//     public void Build(MapGraph g)
//     {
//         // Temizle
//         foreach (Transform c in canvasArea) Destroy(c.gameObject);
//         _nodeViews.Clear();

//         var size = canvasArea.rect.size;
//         float cols = g.layers;
//         float colW = (size.x - padding*2) / Mathf.Max(1, cols - 1);

//         for (int layer = 0; layer < g.layers; layer++)
//         {
//             var nodes = g.GetLayer(layer);
//             int count = Mathf.Max(1, nodes.Count);
//             float rowH = (size.y - padding*2) / Mathf.Max(1, count - 1);

//             for (int i = 0; i < nodes.Count; i++)
//             {
//                 var n = nodes[i];
//                 var go = Instantiate(nodeButtonPrefab, canvasArea);
//                 go.name = $"Node_{n.id}_{n.kind}_{layer}";
//                 _nodeViews[n.id] = go;

//                 var rt = go.GetComponent<RectTransform>();
//                 float x = padding + colW * layer;
//                 float y = (count == 1) ? size.y * 0.5f
//                                        : padding + rowH * i;
//                 rt.anchoredPosition = new Vector2(x - size.x*0.5f, y - size.y*0.5f);

//                 // Label
//                 var txt = go.GetComponentInChildren<TMP_Text>();
//                 if (txt) txt.text = n.kind.ToString();

//                 // Button
//                 var btn = go.GetComponent<Button>();
//                 int captured = n.id;
//                 btn.onClick.AddListener(() => OnNodeClicked(captured));
//             }
//         }

//         RefreshColors();
//         // Kenar çizimleri için: basitçe LineRenderer/UI bağlantısı ekleyebilirsin
//         // (Her child için çizgi çek; burada temel mantığı bıraktım.)
//     }

//     public void RefreshColors()
//     {
//         foreach (var kv in _nodeViews)
//         {
//             int id = kv.Key;
//             var go = kv.Value;
//             var img = go.GetComponent<Image>();
//             if (!img) continue;

//             if (P.Visited.Contains(id))
//                 img.color = visitedColor;
//             else if (P.Unlocked.Contains(id))
//                 img.color = unlockedColor;
//             else
//                 img.color = lockedColor;

//             // Current node’u vurgulamak istersen:
//             if (id == P.CurrentNodeId)
//                 img.color = Color.yellow;
//         }
//     }

//     private void OnNodeClicked(int id)
//     {
//         if (P.CanMoveTo(id))
//         {
//             P.CompleteCurrentNodeAndMoveTo(id);
//             RefreshColors();
//         }
//         else
//         {
//             Debug.Log("Node locked or invalid.");
//         }
//     }
// }
