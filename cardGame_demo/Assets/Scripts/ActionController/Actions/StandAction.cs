// StandAction.cs
public class StandAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public StandAction(Actor a, PhaseKind p) { actor = a; phase = p; }

    public void Execute(CombatContext ctx)
    {
        var acc = ctx.GetAcc(actor, phase);
        acc.Stand(ctx.Threshold);

        // UI/bridge event’lerini tetikle
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.Threshold);

        // (İstersen log)
        ctx.OnLog?.Invoke($"[{actor}:{phase}] STAND → {acc.Total}");
    }

    public string Describe() => $"Stand({actor},{phase})";
}
