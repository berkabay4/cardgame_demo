public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k)
    {
        actor = a;
        phase = k;
    }

    public void Execute(CombatContext ctx)
    {
        // 1) Aktörün kendi destesini al
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck found for {actor}. Draw skipped.");
            return;
        }

        // 2) Accumulator'ı al ve eski değeri not et
        var acc = ctx.GetAcc(actor, phase);
        int before = acc.Total;

        // 3) Kart çekip uygula (PhaseAccumulator kendi içinde deck'ten çekecek)
        acc.Hit(deck, ctx.Threshold);

        // 4) UI/Event köprüsü
        var lastCard = acc.Cards.Count > 0 ? acc.Cards[acc.Cards.Count - 1] : default;
        ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.Threshold);
        ctx.OnLog?.Invoke($"[{actor}:{phase}] {before} → {acc.Total}");
    }

    public string Describe() => $"Draw({actor},{phase})";
}
