using UnityEngine;
using System.Collections;

public abstract class MiniBossAttackBehaviour : ScriptableObject
{
    /// <summary>
    /// MiniBoss / Boss saldırı davranışı.
    /// - anim: saldırı animasyonlarını oynatmak için köprü
    /// - ctx: CombatContext (player, deckler vs.)
    /// - enemy: saldıran SimpleCombatant (MiniBossRuntime taşıyan)
    /// - baseAttackValue: o elde DEF sonrası kalan efektif damage
    /// - info: fight tipi, kaçıncı el, elde kaçıncı enemy saldırısı
    /// </summary>
    public abstract IEnumerator ExecuteAttackCoroutine(
        IAnimationBridge anim,
        CombatContext ctx,
        SimpleCombatant enemy,
        int baseAttackValue,
        EnemyAttackContextInfo info
    );
}
