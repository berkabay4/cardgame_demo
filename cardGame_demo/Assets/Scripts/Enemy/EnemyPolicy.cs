// EnemyPolicy.cs
using System.Collections.Generic;
using UnityEngine;

public static class EnemyPolicy
{
    public static IEnumerator<IGameAction> BuildPhaseEnumerator(CombatContext ctx, PhaseKind phase)
    {
        var enemy = ctx.GetUnit(Actor.Enemy); // SimpleCombatant
        var provider = enemy ? enemy.GetComponent<IEnemyTargetRangeProvider>() as IEnemyTargetRangeProvider : null;

        // Hedef aralığı ve hard cap'i al
        var target = provider != null ? provider.GetRange(phase, ctx.Threshold)
                                      : GetFallbackRange(phase, ctx.Threshold);

        int cap = provider != null ? (provider as EnemyTargetRangeProvider).GetMaxRangeCap(ctx.Threshold)
                                   : ctx.Threshold;

        // Hedef aralığını cap'e kıstır
        target.min = Mathf.Clamp(target.min, 0, cap);
        target.max = Mathf.Clamp(target.max, 0, cap);
        if (target.max < target.min) (target.min, target.max) = (target.max, target.min);

        var acc = ctx.GetAcc(Actor.Enemy, phase);

        while (!acc.IsBusted && !acc.IsStanding)
        {
            int t = acc.Total;

            // --- YENİ KURAL: cap'e ulaştıysa artık kart çekme, hemen STAND ---
            if (t >= cap)
            {
                yield return new StandAction(Actor.Enemy, phase);
                yield break;
            }

            // Hedef penceresindeyse STAND
            if (t >= target.min && t <= target.max)
            {
                yield return new StandAction(Actor.Enemy, phase);
                yield break;
            }

            // Aksi halde kart çek
            yield return new DrawCardAction(Actor.Enemy, phase);
        }
        // bust/stand ile çıkar
    }

    static (int min,int max) GetFallbackRange(PhaseKind phase, int threshold)
    {
        if (phase == PhaseKind.Attack)
        {
            int min = Mathf.Clamp(threshold - 7, 0, threshold);
            int max = Mathf.Clamp(threshold - 3, 0, threshold);
            if (max < min) (min, max) = (max, min);
            return (min, max);
        }
        else
        {
            int min = Mathf.Clamp(threshold - 9, 0, threshold);
            int max = Mathf.Clamp(threshold - 5, 0, threshold);
            if (max < min) (min, max) = (max, min);
            return (min, max);
        }
    }
}
