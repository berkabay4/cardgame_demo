// HealthRouter.cs
using TMPro;
using UnityEngine;

public class HealthRouter : MonoBehaviour
{
    [Header("Tags (scene)")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string enemyTag  = "Enemy";

    [Header("Targets (UI)")]
    [SerializeField] private TMP_Text playerHpText; // "current / max"
    [SerializeField] private TMP_Text enemyHpText;  // "current / max"

    [Header("Format & Update")]
    [SerializeField] private string format = "{0} / {1}";
    [SerializeField, Min(0f)] private float updateInterval = 0.1f;
    [SerializeField] private bool logWarnings = true;

    private SimpleCombatant playerSC;
    private SimpleCombatant enemySC;
    private float timer;

    private void Awake()
    {
        // Tag'li GO'ları bul
        var pGo = GameObject.FindGameObjectWithTag(playerTag);
        var eGo = GameObject.FindGameObjectWithTag(enemyTag);

        if (!pGo && logWarnings) Debug.LogWarning($"[HealthRouter] No GameObject with tag '{playerTag}' found.");
        if (!eGo && logWarnings) Debug.LogWarning($"[HealthRouter] No GameObject with tag '{enemyTag}' found.");

        // Üzerinde veya child'ında SimpleCombatant ara
        if (pGo)
            playerSC = pGo.GetComponent<SimpleCombatant>() ?? pGo.GetComponentInChildren<SimpleCombatant>(true);
        if (eGo)
            enemySC = eGo.GetComponent<SimpleCombatant>() ?? eGo.GetComponentInChildren<SimpleCombatant>(true);

        if (!playerSC && logWarnings) Debug.LogWarning("[HealthRouter] Player SimpleCombatant not found.");
        if (!enemySC && logWarnings) Debug.LogWarning("[HealthRouter] Enemy  SimpleCombatant not found.");

        // İlk görünüm (0. frame’de)
        RefreshTexts();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (playerSC && playerHpText)
            playerHpText.SetText(format, playerSC.CurrentHP, playerSC.MaxHP);

        if (enemySC && enemyHpText)
            enemyHpText.SetText(format, enemySC.CurrentHP, enemySC.MaxHP);
    }

    // İstersen dışarıdan manuel tetiklemek için:
    public void ForceRefresh() => RefreshTexts();
}
