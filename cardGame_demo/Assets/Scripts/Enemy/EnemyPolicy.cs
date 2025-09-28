using System.Collections.Generic;
using UnityEngine;

public static class EnemyPolicy
{
    public static IEnumerator<IGameAction> BuildPhaseEnumerator(CombatContext ctx, PhaseKind phase)
    {
        var enemy = ctx.GetUnit(Actor.Enemy);                 // aktif düşman (SetEnemy ile atanmış)
        var acc   = ctx.GetAcc(Actor.Enemy, phase);

        // Faz zaten bitmişse enumerator üretme
        if (acc.IsBusted || acc.IsStanding)
            yield break;

        // --- Range & Cap sağlayıcıyı güvenli çöz ---
        IEnemyTargetRangeProvider provider = null;
        EnemyTargetRangeProvider   concrete = null;
        if (enemy)
        {
            // Interface ile dene (tercih)
            provider = enemy.GetComponent<IEnemyTargetRangeProvider>();
            // Cap için concrete gerekiyorsa ayrı çöz
            concrete = enemy.GetComponent<EnemyTargetRangeProvider>();
        }

        // Hedef aralığı
        (int min, int max) target = provider != null
            ? provider.GetRange(phase, ctx.Threshold)
            : GetFallbackRange(phase, ctx.Threshold);

        // Hard cap
        int cap = ctx.Threshold;
        if (concrete != null)
            cap = Mathf.Max(0, concrete.GetMaxRangeCap(ctx.Threshold));

        // Hedef aralığını cap’e kıstır
        target.min = Mathf.Clamp(target.min, 0, cap);
        target.max = Mathf.Clamp(target.max, 0, cap);
        if (target.max < target.min) (target.min, target.max) = (target.max, target.min);

        // --- Ana döngü ---
        int safety = 0;
        const int MAX_STEPS = 64; // sonsuz döngü koruması

        while (!acc.IsBusted && !acc.IsStanding)
        {
            int t = acc.Total;

            // cap'e ulaştı/üstündeyse: dur
            if (t >= cap)
            {
                yield return new StandAction(Actor.Enemy, phase);
                yield break;
            }

            // hedef penceresi içindeyse: dur
            if (t >= target.min && t <= target.max)
            {
                yield return new StandAction(Actor.Enemy, phase);
                yield break;
            }

            // Aksi halde kart çek
            yield return new DrawCardAction(Actor.Enemy, phase);

            // Safety
            if (++safety > MAX_STEPS)
            {
                ctx.OnLog?.Invoke($"[AI] Safety break on {phase} (>{MAX_STEPS} steps). Forcing STAND.");
                yield return new StandAction(Actor.Enemy, phase);
                yield break;
            }
        }

        // Bust/Stand ile döngü dışına düştüyse zaten faz bitmiş demektir
        yield break;
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
