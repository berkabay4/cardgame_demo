// StartTurnAction.cs
using System.Linq;
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
        //    Kural: Deck boşaldığında rebuild/shuffle sadece DrawCardAction içinde yapılır.

        // 2) O anda context'te bulunan TÜM aktörlerin faz akümülatörlerini resetle
        //    (Phases anahtarlarından benzersiz Actor setini çıkarıyoruz)
        var actors = new HashSet<Actor>(ctx.Phases.Keys.Select(k => k.Item1));
        foreach (var actor in actors)
        {
            ctx.ResetPhases(actor, log: true); // OnProgress(0, per-phase threshold) + log
        }

        // 3) Turn log (global fallback threshold’u bilgi amaçlı yazıyoruz)
        ctx.OnLog?.Invoke($"========== NEW TURN ==========\nThreshold (fallback): {ctx.Threshold}");
    }

    public string Describe() => "StartTurn";
}
