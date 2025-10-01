using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    [DisallowMultipleComponent]
    public class MapPlayerTracker : MonoBehaviour
    {
        [Header("Flow")]
        public bool lockAfterSelecting = false;
        [Min(0f)] public float enterNodeDelay = 1f;

        [Header("Refs")]
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        /// <summary>Seçim ve giriş olayları (köprü buraya bağlanır)</summary>
        [Header("Events")]
        public UnityEvent<MapNode> onNodeSelected = new(); // path’e eklendi, swirl başladı
        public UnityEvent<MapNode> onNodeEntered  = new(); // gecikme bitti, sahne/GUI açılabilir

        public bool Locked { get; private set; }

        void Awake() => Instance = this;

        public void SelectNode(MapNode mapNode)
        {
            if (Locked || mapNode == null) return;

            // Erişilebilirlik kontrolü
            if (mapManager.CurrentMap.path.Count == 0)
            {
                // İlk seçim: row==0 olmalı
                if (mapNode.Node.point.y != 0)
                {
                    PlayWarningThatNodeCannotBeAccessed();
                    return;
                }

                SendPlayerToNode(mapNode);
                return;
            }

            // Devam eden run: mevcut noktadan outgoing’de olmalı
            var currentPoint = mapManager.CurrentMap.path[^1];
            var currentNode  = mapManager.CurrentMap.GetNode(currentPoint);

            if (currentNode != null && currentNode.outgoing.Any(p => p.Equals(mapNode.Node.point)))
            {
                SendPlayerToNode(mapNode);
            }
            else
            {
                PlayWarningThatNodeCannotBeAccessed();
            }
        }

        void SendPlayerToNode(MapNode mapNode)
        {
            if (mapNode == null) return;

            // Kilitle ve path’i güncelle
            Locked = lockAfterSelecting;
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();

            // Görsel güncellemeler
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();

            // Olay: seçildi (köprü isterse burada UI feedback verebilir)
            onNodeSelected?.Invoke(mapNode);

            // Giriş gecikmesi sonra “entered” olayını yayınla
            DOTween.Sequence()
                   .AppendInterval(enterNodeDelay)
                   .OnComplete(() => EnterNode(mapNode));
        }

        void EnterNode(MapNode mapNode)
        {
            // Olay: artık sahne/GUI açabilirsiniz (combat/rest/shop vs.)
            onNodeEntered?.Invoke(mapNode);
            // Not: Lock’ı köprü/sahne geçişi başlatıyorsa açık kalsın.
            // GUI üzerinde kalan akışlarda köprü işini bitirdiğinde Unlock() çağırabilir.
        }

        public void Unlock() => Locked = false; // GUI akışları bitince çağır

        void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("[MapPlayerTracker] Selected node cannot be accessed.");
        }
    }
}
