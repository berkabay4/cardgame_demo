// StartTurnAction.cs
public class StartTurnAction : IGameAction
{
    readonly bool reshuffleWhenLow; readonly int lowDeckCount;
    public StartTurnAction(bool r, int l){ reshuffleWhenLow=r; lowDeckCount=l; }
    public void Execute(CombatContext ctx)
    {
        if (reshuffleWhenLow && ctx.Deck.Count < lowDeckCount) ctx.Deck.RebuildAndShuffle();
        foreach (var kv in ctx.Phases) kv.Value.Reset();
        // tüm fazlar için 0 / threshold yayınla
        ctx.OnProgress.Invoke(Actor.Player, PhaseKind.Defense, 0, ctx.Threshold);
        ctx.OnProgress.Invoke(Actor.Player, PhaseKind.Attack,  0, ctx.Threshold);
        ctx.OnProgress.Invoke(Actor.Enemy,  PhaseKind.Defense, 0, ctx.Threshold);
        ctx.OnProgress.Invoke(Actor.Enemy,  PhaseKind.Attack,  0, ctx.Threshold);
        ctx.OnLog.Invoke($"========== NEW TURN ==========\nThreshold: {ctx.Threshold}");
    }
    public string Describe()=> "StartTurn";
}
