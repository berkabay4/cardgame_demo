using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Elite Enemy/MiniBoss Behaviours/Basic Single Attack")]
public class BasicSingleAttackBehaviour : MiniBossAttackBehaviour
{
    public override void ExecuteAttack(CombatContext ctx, MiniBossRuntime boss)
    {
        if (boss == null || boss.Definition == null) return;

        // Damage aralığını definition'dan çek
        int dmg = boss.Definition.attackDamageRange.RollInclusive();

        // Örnek: Player'a vur
        ctx.DealDamageToPlayer(dmg);

        Debug.Log($"[MiniBoss] {boss.Definition.displayName} basic attack for {dmg} damage!");
    }
}
