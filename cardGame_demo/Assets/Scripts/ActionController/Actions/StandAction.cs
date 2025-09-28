// StandAction.cs
public class StandAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public StandAction(Actor a, PhaseKind k) { actor = a; phase = k; }

    public void Execute(CombatContext ctx)
    {
        var acc = ctx.GetAcc(actor, phase);

        // Fazın eşiğini çek
        int max = ctx.GetThreshold(actor, phase);

        // Sadece standing’e geçir; total'i SIFIRLAMA
        if (!acc.IsStanding)
        {
            acc.Stand(max); // ← gerekli parametre
        }

        // UI: doğru eşikle yayınla
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, max);
        ctx.OnLog?.Invoke($"[{actor}:{phase}] STAND at {acc.Total} (max {max})");
    }

    public string Describe() => $"Stand({actor},{phase})";
}
