using UnityEngine;
using System.Collections;

public abstract class MiniBossAttackBehaviour : ScriptableObject
{
    public abstract IEnumerator ExecuteAttackCoroutine(
        IAnimationBridge animBridge,
        CombatContext ctx,
        MiniBossRuntime boss,
        int baseAttackValue,
        int attackRoundIndex);
}