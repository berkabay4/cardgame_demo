using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Stats (Runtime)")]
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField, Min(0)] private int currentHealth = 20;

    [Space(8)]
    [SerializeField, Min(5)] private int maxAttackRange  = 21;  // ATK için üst sınır
    [SerializeField, Min(5)] private int maxDefenseRange = 21;  // DEF için üst sınır

    [Header("Events")]
    public UnityEvent onInitialized;
    public UnityEvent<int,int> onHealthChanged; // current, max
    public UnityEvent<int> onAttackRangeChanged;
    public UnityEvent<int> onDefenseRangeChanged;

    [Tooltip("DEPRECATED: Tek range olsaydı kullanılırdı. Geriye dönük uyumluluk için ATK değeriyle tetiklenir.")]
    public UnityEvent<int> onMaxRangeChanged;

    // Props
    public int MaxHealth       => maxHealth;
    public int CurrentHealth   => currentHealth;
    public int MaxAttackRange  => maxAttackRange;
    public int MaxDefenseRange => maxDefenseRange;

    public void InitFrom(PlayerData data)
    {
        if (!data) return;

        maxHealth = Mathf.Max(1, data.maxHealth);
        currentHealth = maxHealth;

        // PlayerData'daki çift range'i yükle
        maxAttackRange = Mathf.Max(5, data.maxAttackRange);
        maxDefenseRange = Mathf.Max(5, data.maxDefenceRange);

        onInitialized?.Invoke();
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // Yeni event’ler
        onAttackRangeChanged?.Invoke(maxAttackRange);
        onDefenseRangeChanged?.Invoke(maxDefenseRange);

        // Geriye dönük tek-event (deprecated): ATK değerini yayınlıyoruz
        onMaxRangeChanged?.Invoke(maxAttackRange);
        
        var dir = GameDirector.Instance;
        if (dir && dir.Ctx != null)
        {
            dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack,  maxAttackRange);
            dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, maxDefenseRange);
        }
    }

    // ---- Health helpers ----
    public void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int value, bool refill = false)
    {
        maxHealth = Mathf.Max(1, value);
        if (refill) currentHealth = maxHealth;
        else currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ---- Range helpers ----
    public void SetAttackRange(int value)
    {
        maxAttackRange = Mathf.Max(5, value);
        onAttackRangeChanged?.Invoke(maxAttackRange);
        onMaxRangeChanged?.Invoke(maxAttackRange); // deprecated uyumluluk
    }

    public void SetDefenseRange(int value)
    {
        maxDefenseRange = Mathf.Max(5, value);
        onDefenseRangeChanged?.Invoke(maxDefenseRange);
        // tek-range event’ine dokunmuyoruz
    }

    // İsteğe bağlı: CombatContext’e uygula (varsa)
    public void ApplyRangesToContext()
    {
        var dir = GameDirector.Instance;
        if (dir == null || dir.Ctx == null) return;

        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack,  maxAttackRange);
        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, maxDefenseRange);
    }
}
