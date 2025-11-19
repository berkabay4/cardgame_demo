using UnityEngine;

public abstract class MiniBossAttackBehaviour : ScriptableObject
{
    /// <summary>
    /// Mini boss’un bir turdaki saldırı davranışı.
    /// </summary>
    public abstract void ExecuteAttack(CombatContext ctx, MiniBossRuntime boss);
}
