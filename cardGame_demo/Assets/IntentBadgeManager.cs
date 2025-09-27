// // Scripts/UI/IntentBadgeManager.cs
// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public class IntentBadgeManager : MonoBehaviour
// {
//     [Header("Refs")]
//     [SerializeField] private Canvas battleCanvas;      // Screen-Space Overlay
//     [SerializeField] private Camera cam;               // ana kamera
//     [SerializeField] private IntentBadgeView badgePrefab;
//     [SerializeField] private IntentIconLibrary iconLibrary;

//     private readonly Dictionary<IIntentSource, (IntentBadgeView view, Action unsub)> map = new();

//     void Reset()
//     {
//         if (!battleCanvas) battleCanvas = FindAnyObjectByType<Canvas>();
//         if (!cam) cam = Camera.main;
//     }

//     public void Register(IIntentSource src)
//     {
//         if (src == null || map.ContainsKey(src)) return;

//         var view = Instantiate(badgePrefab, battleCanvas.transform);
//         var follow = view.gameObject.AddComponent<UiFollowWorld>();
//         follow.worldTarget = src.WorldAnchor;
//         follow.cam = cam;

//         void Refresh() => Apply(view, src);

//         src.OnIntentChanged += Refresh;
//         map[src] = (view, () => src.OnIntentChanged -= Refresh);

//         Refresh();
//     }

//     public void Unregister(IIntentSource src)
//     {
//         if (!map.TryGetValue(src, out var data)) return;
//         data.unsub?.Invoke();
//         if (data.view) Destroy(data.view.gameObject);
//         map.Remove(src);
//     }

//     private void Apply(IntentBadgeView view, IIntentSource src)
//     {
//         if (!view) return;
//         var sprite = iconLibrary ? iconLibrary.Get(src.IntentKey) : null;
//         view.SetData(sprite, src.AttackPerHit, src.AttackHits, src.BlockGain);
//     }
// }
