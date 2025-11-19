using UnityEngine;

[DisallowMultipleComponent]
public class MiniBossRuntime : MonoBehaviour
{
    [SerializeField] private MiniBossDefinition definition;
    public MiniBossDefinition Definition => definition;

    public int CurrentHealth { get; private set; }

    public void Init(MiniBossDefinition def)
    {
        definition = def;
        CurrentHealth = def != null ? def.maxHealth : 1;
        // burada HP bar vs. bağlayabilirsin
    }

    public void TakeTurn(CombatContext ctx)
    {
        if (definition != null && definition.attackBehaviour != null)
        {
            definition.attackBehaviour.ExecuteAttack(ctx, this);
        }
        else
        {
            // Fallback: basit auto-attack
            int dmg = definition != null ? definition.attackDamageRange.RollInclusive() : 5;
            ctx.DealDamageToPlayer(dmg);
            Debug.Log($"[MiniBoss] {name} fallback attack for {dmg} damage (no behaviour set).");
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        // öldü mü, ölüm animasyonu vs.
    }
}
