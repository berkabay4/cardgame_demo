public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k){ actor=a; phase=k; }

    public void Execute(CombatContext ctx)
    {
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck for {actor}. Draw skipped.");
            return;
        }

        // --- KRİTİK KURAL: Boşsa önce shuffle, bu aksiyonda kart çekme ---
        if (deck.Count == 0)
        {
            deck.RebuildAndShuffle();
            ctx.OnLog?.Invoke($"[Deck] Empty → Rebuilt+Shuffled for {actor}. Draw will happen on next request.");
            return;
        }

        var acc = ctx.GetAcc(actor, phase);
        int before = acc.Total;

        // Eğer PhaseAccumulator.Hit(deck, threshold) varsa:
        acc.Hit(deck, ctx.Threshold);

        // Yoksa:
        // var card = deck.Draw();
        // acc.Add(card, ctx.Threshold);

        var lastCard = acc.Cards.Count > 0 ? acc.Cards[^1] : default;
        ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.Threshold);
        ctx.OnLog?.Invoke($"[{actor}:{phase}] {before} → {acc.Total}");
    }

    public string Describe()=> $"Draw({actor},{phase})";
}
