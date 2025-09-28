// StartTurnAction.cs
public class StartTurnAction : IGameAction
{
    readonly bool reshuffleWhenLow;  // imza korunuyor
    readonly int  lowDeckCount;      // imza korunuyor

    public StartTurnAction(bool r, int l)
    {
        reshuffleWhenLow = r;
        lowDeckCount     = l;
    }

    public void Execute(CombatContext ctx)
    {
        // 1) Destelere dokunma (kural: boşalınca Draw tarafında rebuild/shuffle)
        //    -> Burada hiçbir deck işlemi yok.

        // 2) Faz accumulator'larını resetle
        // CombatContext.ResetPhases(...) zaten OnProgress(0,Threshold) yayınlıyor (biz log=true veriyoruz)
        ctx.ResetPhases(Actor.Player, log: true);
        ctx.ResetPhases(Actor.Enemy,  log: true);

        // 3) Turn log
        ctx.OnLog?.Invoke($"========== NEW TURN ==========\nThreshold: {ctx.Threshold}");
    }

    public string Describe() => "StartTurn";
}
