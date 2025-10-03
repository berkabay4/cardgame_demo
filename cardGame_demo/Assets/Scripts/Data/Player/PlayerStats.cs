using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization; // FormerlySerializedAs

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Health (Runtime)")]
    [SerializeField, Min(1)]  private int maxHealth = 20;
    [SerializeField, Min(0)]  private int currentHealth = 20;

    [Space(8)]
    [Header("Thresholds (Runtime)")]
    // Eski sahne/prefab verilerini korumak için:
    [FormerlySerializedAs("maxAttackRange")]
    [SerializeField, Min(5)]  private int attackThreshold = 21;   // Oyuncunun ATK eşiği (arttırılabilir)
    [FormerlySerializedAs("maxDefenseRange")]
    [SerializeField, Min(5)]  private int defenseThreshold = 21;  // Oyuncunun DEF eşiği (arttırılabilir)

    [Header("Events")]
    public UnityEvent onInitialized;
    public UnityEvent<int,int> onHealthChanged; // (current, max/kapasite)
    public UnityEvent<int> onAttackRangeChanged;   // ATK eşiği değişti
    public UnityEvent<int> onDefenseRangeChanged;  // DEF eşiği değişti

    [Tooltip("DEPRECATED: Tek range olsaydı kullanılırdı. Geriye dönük uyumluluk için ATK değeriyle tetiklenir.")]
    public UnityEvent<int> onMaxRangeChanged;      // (deprecated) hâlâ ATK değeriyle tetiklenir

    // === Public Readonly (geri-uyum) ===
    public int MaxHealth        => maxHealth;
    public int CurrentHealth    => currentHealth;

    // Geriye dönük isimler korunuyor:
    public int MaxAttackRange   => attackThreshold;
    public int MaxDefenseRange  => defenseThreshold;

    // İsimsel olarak daha doğru “yeni” getter’lar:
    public int AttackThreshold  => attackThreshold;
    public int DefenseThreshold => defenseThreshold;

    // === Init ===
    public void InitFrom(PlayerData data)
    {
        if (!data) return;

        maxHealth      = Mathf.Max(1, data.maxHealth);
        currentHealth  = maxHealth;

        // Çift eşiği yükle (üst sınır/cap yok; sadece mantıklı alt sınır)
        attackThreshold  = Mathf.Max(5, data.maxAttackRange);
        defenseThreshold = Mathf.Max(5, data.maxDefenceRange);

        onInitialized?.Invoke();
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // Event’ler
        onAttackRangeChanged?.Invoke(attackThreshold);
        onDefenseRangeChanged?.Invoke(defenseThreshold);
        onMaxRangeChanged?.Invoke(attackThreshold); // (deprecated)

        // CombatContext varsa anında uygula
        var dir = CombatDirector.Instance;
        if (dir && dir.Ctx != null)
        {
            dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack,  attackThreshold);
            dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, defenseThreshold);
        }
    }

    // === Health helpers ===
    public void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int value, bool refill = false)
    {
        maxHealth = Mathf.Max(1, value);
        if (refill) currentHealth = maxHealth;
        else        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // === Threshold helpers (yeni, isimleri net) ===
    public void SetAttackThreshold(int value)
    {
        attackThreshold = Mathf.Max(5, value); // alt sınır
        onAttackRangeChanged?.Invoke(attackThreshold);
        onMaxRangeChanged?.Invoke(attackThreshold); // (deprecated)

        ApplyAttackToContextIfAlive();
    }

    public void SetDefenseThreshold(int value)
    {
        defenseThreshold = Mathf.Max(5, value); // alt sınır
        onDefenseRangeChanged?.Invoke(defenseThreshold);

        ApplyDefenseToContextIfAlive();
    }

    public void AddAttackThreshold(int delta)
        => SetAttackThreshold(attackThreshold + delta);

    public void AddDefenseThreshold(int delta)
        => SetDefenseThreshold(defenseThreshold + delta);

    // === Geriye dönük setter isimleri (eski kod kırılmasın) ===
    public void SetAttackRange(int value)  => SetAttackThreshold(value);
    public void SetDefenseRange(int value) => SetDefenseThreshold(value);

    // === CombatContext’e uygula (isteğe bağlı genel çağrı) ===
    public void ApplyRangesToContext()
    {
        var dir = CombatDirector.Instance;
        if (dir == null || dir.Ctx == null) return;

        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack,  attackThreshold);
        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, defenseThreshold);
    }

    // === Internal convenience ===
    void ApplyAttackToContextIfAlive()
    {
        var dir = CombatDirector.Instance;
        if (dir?.Ctx == null) return;

        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack, attackThreshold);
        // UI tazele
        dir.Ctx.OnProgress?.Invoke(
            Actor.Player, PhaseKind.Attack,
            dir.State?.PlayerAtkTotal ?? 0,
            attackThreshold
        );
    }

    void ApplyDefenseToContextIfAlive()
    {
        var dir = CombatDirector.Instance;
        if (dir?.Ctx == null) return;

        dir.Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, defenseThreshold);
        // UI tazele
        dir.Ctx.OnProgress?.Invoke(
            Actor.Player, PhaseKind.Defense,
            dir.State?.PlayerDefTotal ?? 0,
            defenseThreshold
        );
    }
}
