using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthRouter : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] private SimpleCombatant self;               // Player'ın SimpleCombatant'ı
    [SerializeField] private TextMeshProUGUI hpText;             // Canvas child[2] üzerindeki TMP

    [Header("Format & Update")]
    [SerializeField] private string format = "{0} / {1}";
    [SerializeField, Min(0f)] private float updateInterval = 0.1f;
    [SerializeField] private bool logWarnings = true;

    private float _timer;

    void Reset() => AutoWire();

    void Awake()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
        if (!hpText) AutoWire();
        ForceRefresh();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;
        Refresh();
    }

    void AutoWire()
    {
        if (!self) self = GetComponent<SimpleCombatant>();

        // Altındaki Canvas'ı bul ve child[2]'den TMP çek
        var canvasTf = GetComponentInChildren<Canvas>(true)?.transform ?? transform.Find("Canvas");
        if (!canvasTf)
        {
            if (logWarnings) Debug.LogWarning("[PlayerHealthRouter] Canvas not found under Player.");
            return;
        }
        if (canvasTf.childCount <= 2)
        {
            if (logWarnings) Debug.LogWarning("[PlayerHealthRouter] Canvas needs at least 3 children (DEF, ATK, HP).");
            return;
        }

        var hpChild = canvasTf.GetChild(2);
        hpText = hpChild.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!hpText && logWarnings) Debug.LogWarning("[PlayerHealthRouter] TMP on Canvas child[2] not found.");
    }

    void Refresh()
    {
        if (self && hpText) hpText.SetText(format, self.CurrentHP, self.MaxHP);
    }

    public void ForceRefresh() => Refresh();
}
