using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RelicBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RelicManager relics;                 // otomatik bulunur
    [SerializeField] private RectTransform content;               // Horizontal/Grid parent
    [SerializeField] private RelicIconView iconPrefab;            // tek ikon prefab

    [Header("Layout (Optional)")]
    [SerializeField] private bool autoAddLayoutGroup = true;
    [SerializeField] private Vector2 padding = new Vector2(6, 6);
    [SerializeField] private float spacing = 6f;

    // pool
    private readonly List<RelicIconView> _pool = new();
    private int _activeCount = 0;

    private void Reset()
    {
        if (!relics) relics = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (!content) content = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (!relics) relics = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (!content) content = GetComponent<RectTransform>();

        if (autoAddLayoutGroup && content && !content.GetComponent<HorizontalLayoutGroup>())
        {
            var hg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hg.padding = new RectOffset((int)padding.x, (int)padding.x, (int)padding.y, (int)padding.y);
            hg.spacing = spacing;
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
        }
    }

    private void OnEnable()
    {
        Subscribe(true);
        Rebuild();
    }

    private void OnDisable() => Subscribe(false);

    private void Subscribe(bool on)
    {
        if (!relics) return;
        if (on) relics.OnRelicsChanged += HandleRelicsChanged;
        else    relics.OnRelicsChanged -= HandleRelicsChanged;
    }

    private void HandleRelicsChanged() => Rebuild();

    public void Rebuild()
    {
        if (!relics || iconPrefab == null || content == null) return;

        var list = relics.All; // IEnumerable<RelicRuntime>

        // Açık ikonları kapat
        for (int i = 0; i < _activeCount; i++) _pool[i].gameObject.SetActive(false);
        _activeCount = 0;

        foreach (var rr in list)
        {
            var view = GetView();
            view.gameObject.SetActive(true);
            view.Bind(rr);
            _activeCount++;
        }
    }

    private RelicIconView GetView()
    {
        // Havuzda pasif var mı?
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].gameObject.activeSelf) return _pool[i];

        // Yoksa oluştur
        var inst = Instantiate(iconPrefab, content);
        _pool.Add(inst);
        return inst;
    }
}
