using UnityEngine;

[DisallowMultipleComponent]
public class SimpleCombatant : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private HealthManager health;   // zorunlu

    [Header("Combat Runtime")]
    [SerializeField] private int currentAttack; // Bu eli vuracağı ATK (Accept sonrası set et)

    // === Eski alanlar kaldırıldı; geçiş sürecinde Inspector uyumu için...
    [System.Obsolete("Use HealthManager.MaxHP")]
    [SerializeField, HideInInspector] public int maxHP = 80;

    // --- Properties ---
    public int MaxHP         => health ? health.MaxHP : 0;
    public int CurrentHP     => health ? health.CurrentHP : 0;
    public int Block         => health ? health.CurrentBlock : 0;
    public int CurrentAttack { get => currentAttack; set => currentAttack = value; }

    void Reset()
    {
        if (!health) health = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (!health) health = gameObject.AddComponent<HealthManager>();
    }

    void Awake()
    {
        if (!health) health = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (!health) health = gameObject.AddComponent<HealthManager>();

        // // İstersen burada HealthManager event’lerini dinleyip log atabilirsin:
        // health.OnHPChanged.AddListener(hp =>
        //     Debug.Log($"[{name}] HP -> {hp}/{health.MaxHP}"));
        // health.OnBlockChanged.AddListener(b =>
        //     Debug.Log($"[{name}] BLOCK -> {b}"));
        // health.OnDamaged.AddListener((applied, blocked) =>
        //     Debug.Log($"[{name}] Took {applied} dmg (blocked {blocked})."));
        // health.OnDeath.AddListener(() =>
        //     Debug.Log($"[{name}] DIED"));
    }

    // === Public API (delege) ===
    public void GainBlock(int amount)         => health?.GainBlock(amount);
    public void ClearBlock()                  => health?.ClearBlock();
    public void TakeDamage(int amount)        => health?.TakeDamage(amount);
    public void Heal(int amount)              => health?.Heal(amount);

    public void ApplyFromStats(PlayerStats stats, bool refillToMax = true)
    {
        if (!stats || !health) return;

        int targetMax = Mathf.Max(1, stats.MaxHealth);
        health.SetMaxHP(targetMax, keepRatio: !refillToMax);
        if (refillToMax) health.RefillToMax();
    }

    // İhtiyaç olursa dışarıya set erişimi:
    public void SetMaxHP(int value, bool keepRatio = true) => health?.SetMaxHP(value, keepRatio);
    public void SetHP(int value)                            => health?.SetHP(value);
    public void SetBlock(int value)                         => health?.SetBlock(value);
}
