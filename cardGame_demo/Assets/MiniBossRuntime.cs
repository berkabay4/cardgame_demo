using UnityEngine;

[DisallowMultipleComponent]
public class MiniBossRuntime : MonoBehaviour
{
    [SerializeField] private MiniBossDefinition definition;
    public MiniBossDefinition Definition => definition;

    /// <summary>Bilgi amaçlı; gerçek HP HealthManager üzerinden tutulur.</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>Combat’a spawn olurken çağır.</summary>
    public void Init(MiniBossDefinition def)
    {
        definition = def;

        int maxHp = (def != null) ? Mathf.Max(1, def.maxHealth) : 1;

        // HealthManager ile senkronize et
        var hm = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.SetMaxHP(maxHp, keepRatio: false);
            hm.RefillToMax();
            CurrentHealth = hm.CurrentHP;
        }
        else
        {
            // HM yoksa sadece local değeri güncelle
            CurrentHealth = maxHp;
        }
    }

    /// <summary>
    /// Enemy attack fazı için çağrılır.
    /// baseAttackValue: Bu elde enemy ATK fazının toplamı (örn: 5/20 çektiyse 5).
    /// attackRoundIndex: Kaçıncı enemy saldırı turu (1,2,3,...) — pattern için kullanılır.
    /// </summary>
    public void TakeTurn(CombatContext ctx, int baseAttackValue, int attackRoundIndex)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[MiniBossRuntime] TakeTurn ctx null.");
            return;
        }

        if (definition != null && definition.attackBehaviour != null)
        {
            definition.attackBehaviour.ExecuteAttack(ctx, this, baseAttackValue, attackRoundIndex);
        }
        else
        {
            // Fallback: davranış yoksa tek vuruş yap
            int dmg = baseAttackValue > 0
                ? baseAttackValue
                : (definition != null ? definition.attackDamageRange.RollInclusive() : 5);

            DealDamageToPlayer(ctx, dmg);
            Debug.Log($"[MiniBossRuntime] {name} fallback attack → {dmg} damage.");
        }
    }

    /// <summary>Mini boss hasar aldığında çağır (örn. Resolution tarafı).</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        var hm = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.TakeDamage(amount);
            CurrentHealth = hm.CurrentHP;
        }
        else
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        }

        // Ölüm kontrolü / animasyon vs. buraya gelebilir.
    }

    // === INTERNAL HELPERS ===

    void DealDamageToPlayer(CombatContext ctx, int amount)
    {
        if (ctx == null || amount <= 0) return;

        var playerUnit = ctx.Player;
        if (playerUnit == null)
        {
            Debug.LogWarning("[MiniBossRuntime] Player unit not found in CombatContext.");
            return;
        }

        var hm = playerUnit.GetComponent<HealthManager>();
        if (hm == null)
        {
            Debug.LogWarning("[MiniBossRuntime] Player HealthManager not found.");
            return;
        }

        hm.TakeDamage(amount);
    }
}
