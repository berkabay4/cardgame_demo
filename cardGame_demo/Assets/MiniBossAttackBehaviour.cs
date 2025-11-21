using UnityEngine;
using System.Collections;

public abstract class MiniBossAttackBehaviour : ScriptableObject
{
    public abstract IEnumerator ExecuteAttackCoroutine(
        IAnimationBridge anim,
        CombatContext ctx,
        SimpleCombatant enemy,
        int baseAttackValue,
        EnemyAttackContextInfo info
    );
}
