using UnityEngine;

public abstract class MiniBossAttackBehaviour : ScriptableObject
{
    /// <param name="baseAttackValue">
    ///   Bu elde enemy ATK fazının toplamı (ör: 5/20 çektiyse 5).
    /// </param>
    /// <param name="attackRoundIndex">
    ///   Kaçıncı enemy saldırı turu (1,2,3,...). CombatDirector’dan geliyor.
    /// </param>
    public abstract void ExecuteAttack(
        CombatContext ctx,
        MiniBossRuntime boss,
        int baseAttackValue,
        int attackRoundIndex
    );
}
