using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RelicBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RelicManager relics;      // auto bulunur
    [SerializeField] private RectTransform content;    // bu objenin RT'si olabilir
    [SerializeField] private RelicIconView iconPrefab; // bir adet ikon prefab

    [Header("Layout")]
    [SerializeField] private bool autoAddLayoutGroup = true;
    [SerializeField] private Vector2 padding = new Vector2(6, 6);
    [SerializeField] private float spacing = 6f;

    // simple pool
    private readonly List<RelicIconView> pool = new();
    private int activeCount = 0;

    void Reset()
    {
        if (!relics) relics = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (!content) content = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (!relics) relics = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (!content) content = GetComponent<RectTransform>();

        if (autoAddLayoutGroup && content && !content.GetComponent<HorizontalLayoutGroup>())
        {
            var hg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hg.padding = new RectOffset((int)padding.x, (int)padding.x, (int)padding.y, (int)padding.y);
            hg.spacing = spacing;
            hg.childAlignment = TextAnchor.MiddleLeft;     // ← soldan sağa
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
        }
    }

    void OnEnable()
    {
        if (relics != null) relics.OnRelicsChanged += HandleRelicsChanged;
        Rebuild();
    }

    void OnDisable()
    {
        if (relics != null) relics.OnRelicsChanged -= HandleRelicsChanged;
    }

    void HandleRelicsChanged() => Rebuild();

    public void Rebuild()
    {
        if (relics == null || iconPrefab == null || content == null) return;

        // kapat
        for (int i = 0; i < activeCount; i++) pool[i].gameObject.SetActive(false);
        activeCount = 0;

        // sırayı koru (Acquire sırası). İstersen rarity’ye göre OrderBy yapabilirsin.
        foreach (var rr in relics.All)
        {
            var view = GetView();
            view.gameObject.SetActive(true);
            view.Bind(rr);
            activeCount++;
        }
    }

    RelicIconView GetView()
    {
        for (int i = 0; i < pool.Count; i++)
            if (!pool[i].gameObject.activeSelf) return pool[i];

        var inst = Instantiate(iconPrefab, content);
        pool.Add(inst);
        return inst;
    }
}
