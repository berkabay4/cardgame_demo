using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "CardGame/Enemy/Elite Enemy/MiniBoss Behaviours/Basic Multi-Hit")]
public class BasicSingleAttackBehaviour : MiniBossAttackBehaviour
{
    [Tooltip("Her kaç elde bir multi-hit devreye girsin? Örn: 2 => 2,4,6... elde.")]
    public int multiHitEveryNRounds = 2;

    [Tooltip("Multi-hit olduğunda kaç vuruş yapsın? Örn: 3 = 3x vurur.")]
    public int multiHitCount = 3;

    public override IEnumerator ExecuteAttackCoroutine(
        IAnimationBridge animBridge,
        CombatContext ctx,
        MiniBossRuntime boss,
        int baseAttackValue,
        int attackRoundIndex)
    {
        if (ctx == null || boss == null || boss.Definition == null)
            yield break;

        // 0 ise fallback için definition’daki damage aralığından çek
        if (baseAttackValue <= 0)
            baseAttackValue = boss.Definition.attackDamageRange.RollInclusive();

        bool doMulti =
            multiHitEveryNRounds > 0 &&
            (attackRoundIndex % multiHitEveryNRounds == 0);

        var playerUnit = ctx.Player;
        if (playerUnit == null) yield break;

        if (!doMulti)
        {
            // Tek vuruş
            yield return PlayOneHit(animBridge, ctx, boss, playerUnit, baseAttackValue);
        }
        else
        {
            // Çoklu vuruş (örnek: 3x 5 damage → 3 defa animasyon + 3 defa 5 damage)
            for (int i = 0; i < multiHitCount; i++)
            {
                yield return PlayOneHit(animBridge, ctx, boss, playerUnit, baseAttackValue);
            }
        }
    }

    IEnumerator PlayOneHit(
        IAnimationBridge animBridge,
        CombatContext ctx,
        MiniBossRuntime boss,
        SimpleCombatant playerUnit,
        int damage)
    {
        var playerHM = playerUnit.GetComponent<HealthManager>();
        if (playerHM == null) yield break;

        // Animasyon + etki
        if (animBridge != null)
        {
            yield return animBridge.PlayAttackAnimation(
                attacker: boss.GetComponent<SimpleCombatant>(),
                defender: playerUnit,
                damage:   damage,
                onImpact: () =>
                {
                    playerHM.TakeDamage(damage);
                });
        }
        else
        {
            // Anim yoksa direkt vur
            playerHM.TakeDamage(damage);
        }

        yield return null;
    }
}
