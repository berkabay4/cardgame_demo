// StartTurnAction.cs
public class StartTurnAction : IGameAction
{
    readonly bool reshuffleWhenLow;
    readonly int  lowDeckCount;

    public StartTurnAction(bool r, int l)
    {
        reshuffleWhenLow = r;
        lowDeckCount     = l;
    }

    public void Execute(CombatContext ctx)
    {
        // 1) Tüm desteler için low-reshuffle
        if (reshuffleWhenLow)
        {
            foreach (var deck in ctx.AllDecks())
            {
                if (deck == null) continue;
                if (deck.Count <= lowDeckCount)
                {
                    deck.RebuildAndShuffle();
                    ctx.OnLog?.Invoke("[Deck] Rebuilt+Shuffled (low count)");
                }
            }
        }

        // 2) Bütün faz accumulator'larını sıfırla
        foreach (var acc in ctx.Phases.Values)
            acc.Reset();

        // 3) Her faz için 0 / threshold progress yayınla
        foreach (var kv in ctx.Phases)
        {
            var (actor, phase) = kv.Key;
            ctx.OnProgress?.Invoke(actor, phase, 0, ctx.Threshold);
        }

        // 4) Log
        ctx.OnLog?.Invoke($"========== NEW TURN ==========\nThreshold: {ctx.Threshold}");
    }

    public string Describe() => "StartTurn";
}
