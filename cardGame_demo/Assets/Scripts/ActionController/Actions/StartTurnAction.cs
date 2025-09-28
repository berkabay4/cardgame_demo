// StartTurnAction.cs
using System.Collections.Generic;

public class StartTurnAction : IGameAction
{
    readonly bool reshuffleWhenLow;  // imza korunuyor (kullanılmıyor)
    readonly int  lowDeckCount;      // imza korunuyor (kullanılmıyor)

    public StartTurnAction(bool r, int l)
    {
        reshuffleWhenLow = r;
        lowDeckCount     = l;
    }

    public void Execute(CombatContext ctx)
    {
        // 1) Deck'lere DOKUNMA.
        //    Kural: Deck boşalınca rebuild/shuffle sadece DrawCardAction içinde yapılır.

        // 2) Context’te gerçekten mevcut olan aktörleri topla (Player/Enemy)
        var actorsToReset = new List<Actor>(2);
        if (ctx.TryGetUnit(Actor.Player, out _)) actorsToReset.Add(Actor.Player);
        if (ctx.TryGetUnit(Actor.Enemy,  out _)) actorsToReset.Add(Actor.Enemy);

        // 3) Her aktör için fazları resetle ve 0 / faz-bazlı max yayınla
        foreach (var actor in actorsToReset)
        {
            // Faz accumulator’larını sıfırla (log:false -> kendi logumuzu atacağız)
            ctx.ResetPhases(actor, log: false);

            // Faz bazlı eşikler
            int defMax = ctx.GetThreshold(actor, PhaseKind.Defense);
            int atkMax = ctx.GetThreshold(actor, PhaseKind.Attack);

            // UI senkronizasyonu
            ctx.OnProgress?.Invoke(actor, PhaseKind.Defense, 0, defMax);
            ctx.OnProgress?.Invoke(actor, PhaseKind.Attack,  0, atkMax);

            // Bilgi logu
            ctx.OnLog?.Invoke($"[Turn] Phases reset for {actor} (DEF max {defMax}, ATK max {atkMax})");
        }

        // 4) Tur başlığı
        ctx.OnLog?.Invoke("========== NEW TURN ==========");
    }

    public string Describe() => "StartTurn";
}
