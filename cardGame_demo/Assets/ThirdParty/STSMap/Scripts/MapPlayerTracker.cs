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
        [Tooltip("Bir düğüm seçildikten sonra yeni seçimleri kilitle")]
        [SerializeField] private bool lockAfterSelecting = false;

        [Tooltip("Seçimden sonra 'giriş' olayı yayınlanmadan önceki gecikme (saniye)")]
        [Min(0f)] public float enterNodeDelay = 1f;

        [Header("Refs")]
        [SerializeField] public MapManager mapManager;
        [SerializeField] public MapView view;

        public static MapPlayerTracker Instance { get; private set; }

        /// <summary>Seçim ve giriş olayları (köprü buraya bağlanır)</summary>
        [Header("Events")]
        [SerializeField] public UnityEvent<MapNode> onNodeSelected = new(); // path’e eklendi, swirl başladı
        [SerializeField] public UnityEvent<MapNode> onNodeEntered  = new(); // gecikme bitti, sahne/GUI açılabilir

        public bool Locked { get; private set; }

        private Sequence _enterSeq;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ResolveRefs();
        }

        private void OnValidate()
        {
            // Editör içinde eksik referansları rahatça toparlar
            if (!Application.isPlaying) ResolveRefs();
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            KillSequence();
        }

        private void ResolveRefs()
        {
            if (!mapManager) mapManager = FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
            if (!view)       view       = mapManager ? mapManager.view : FindFirstObjectByType<MapView>(FindObjectsInactive.Include);
        }

        private void KillSequence()
        {
            if (_enterSeq != null && _enterSeq.IsActive())
            {
                _enterSeq.Kill(false);
                _enterSeq = null;
            }
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked || mapNode == null || mapManager == null || mapManager.CurrentMap == null)
                return;

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

        private void SendPlayerToNode(MapNode mapNode)
        {
            if (mapNode == null) return;

            // Kilitle ve path’i güncelle
            Locked = lockAfterSelecting;
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();

            // Görsel güncellemeler
            if (view != null)
            {
                view.SetAttainableNodes();
                view.SetLineColors();
            }
            mapNode.ShowSwirlAnimation();

            // Olay: seçildi (köprü isterse burada UI feedback verebilir)
            onNodeSelected?.Invoke(mapNode);

            // Varsa önceki sequence’i iptal et
            KillSequence();

            // Giriş gecikmesi: 0 ise anında tetikle
            if (enterNodeDelay <= 0f)
            {
                EnterNode(mapNode);
            }
            else
            {
                _enterSeq = DOTween.Sequence()
                                   .AppendInterval(enterNodeDelay)
                                   .OnComplete(() => EnterNode(mapNode));
            }
        }

        private void EnterNode(MapNode mapNode)
        {
            // Olay: artık sahne/GUI açabilirsiniz (combat/rest/shop vs.)
            onNodeEntered?.Invoke(mapNode);
            // Not: Sahne geçişi yapan köprü Lock’ı yönetebilir;
            // GUI içinde kalıyorsan işin bittiğinde Unlock() çağır.
        }

        /// <summary>GUI akışları bittiğinde harita üstünde tekrar seçim açmak için.</summary>
        public void Unlock() => Locked = false;

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("[MapPlayerTracker] Selected node cannot be accessed.");
        }
    }
}
