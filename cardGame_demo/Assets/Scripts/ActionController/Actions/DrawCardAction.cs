public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k) { actor=a; phase=k; }

    public void Execute(CombatContext ctx)
    {
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck for {actor}. Draw skipped.");
            return;
        }

        var acc = ctx.GetAcc(actor, phase);
        if (acc.IsStanding || acc.IsBusted)
        {
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.GetThreshold(actor, phase)); // ← FIX
            return;
        }

        if (deck.Count == 0)
        {
            deck.RebuildAndShuffle();
            ctx.OnLog?.Invoke($"[Deck] Empty → Rebuilt+Shuffled for {actor}. Drawing now.");
            if (deck.Count == 0) return;
        }

        int before = acc.Total, beforeDeck = deck.Count;

        acc.Hit(deck, ctx.GetThreshold(actor, phase));

        var lastCard = acc.Cards.Count > 0 ? acc.Cards[^1] : default;
        ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.GetThreshold(actor, phase));   // ← FIX
        ctx.OnLog?.Invoke($"[{actor}:{phase}] {before} → {acc.Total} (Deck {beforeDeck}->{deck.Count})");
    }

    public string Describe()=> $"Draw({actor},{phase})";
}
