public class DrawCardAction : IGameAction
{
    readonly Actor actor; readonly PhaseKind phase;
    public DrawCardAction(Actor a, PhaseKind k){ actor=a; phase=k; }

    public void Execute(CombatContext ctx)
    {
        var acc = ctx.GetAcc(actor, phase);
        int before = acc.Total;

        acc.Hit(ctx.Deck, ctx.Threshold); // ← sadece threshold veriyoruz

        var lastCard = acc.Cards.Count>0 ? acc.Cards[^1] : default;
        ctx.OnCardDrawn.Invoke(actor, phase, lastCard);
        ctx.OnProgress.Invoke(actor, phase, acc.Total, ctx.Threshold);
        ctx.OnLog.Invoke($"[{actor}:{phase}] {before} → {acc.Total}");
    }

    public string Describe()=> $"Draw({actor},{phase})";
}
