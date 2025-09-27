// EnemyPolicy.cs
using System.Collections.Generic;

public static class EnemyPolicy
{
    // Faz hedefleri (istersen inspector'a taşıyabilirsin)
    public static (int min, int max) DefenseTarget = (12, 16);
    public static (int min, int max) AttackTarget  = (14, 18);

    // Bu enumerator tek tek aksiyon üretir; her Draw sonrası tekrar değerlendirilir.
    public static IEnumerator<IGameAction> BuildPhaseEnumerator(CombatContext ctx, PhaseKind phase)
    {
        var target = phase == PhaseKind.Defense ? DefenseTarget : AttackTarget;
        var acc = ctx.GetAcc(Actor.Enemy, phase);

        while (!acc.IsBusted && !acc.IsStanding)
        {
            int t = acc.Total;
            if (t >= target.min && t <= target.max)
            {
                yield return new StandAction(Actor.Enemy, phase); // fazı kilitle
                yield break;
            }

            // hedefte değilse kart çek
            yield return new DrawCardAction(Actor.Enemy, phase);

            // Döngü başına döndüğümüzde, acc.Total artık Draw sonucu güncellenmiş olur.
            // (Enumeratör "canlı" çalıştığı için burada ekstra şey yapmaya gerek yok.)
        }

        // Faz, bust ile bittiyse burada doğal olarak çıkar
    }
}
