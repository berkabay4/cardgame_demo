public class StandAction : IGameAction
{
    readonly Actor actor; readonly PhaseKind phase;
    public StandAction(Actor a, PhaseKind k){ actor=a; phase=k; }

    public void Execute(CombatContext ctx)
    {
        var acc = ctx.GetAcc(actor, phase);
        acc.Stand(ctx.Threshold); // ← düz toplam + bust kontrolü içeride
        ctx.OnProgress.Invoke(actor, phase, acc.Total, ctx.Threshold);
        ctx.OnLog.Invoke($"[{actor}:{phase}] STAND = {acc.Total}");
    }

    public string Describe()=> $"Stand({actor},{phase})";
}
