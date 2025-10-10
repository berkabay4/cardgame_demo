// MysteryContext.cs
using UnityEngine;

public class MysteryContext
{
    public readonly MysteryData data;
    public readonly RunContext run;
    public readonly GameSessionDirector director;
    public readonly System.Random rng;

    public MysteryContext(MysteryData data, RunContext run, GameSessionDirector director, System.Random rng)
    {
        this.data = data;
        this.run = run;
        this.director = director;
        this.rng = rng;
    }

    // === Yeni: tamamlanma sinyalleri (Map butonunu açtırır) ===
    public void Complete(MysteryResult result)
        => MysteryManager.RaiseCompleted(result);

    public void CompleteCoins(int coins)
        => Complete(new MysteryResult(GameSessionDirector.MysteryOutcome.Coins, coins));

    public void CompleteNothing()
        => Complete(new MysteryResult(GameSessionDirector.MysteryOutcome.Nothing));

    public void CompleteStartCombat()
        => Complete(new MysteryResult(GameSessionDirector.MysteryOutcome.StartCombat));

    public void CompleteStartTreasure()
        => Complete(new MysteryResult(GameSessionDirector.MysteryOutcome.StartTreasure));
}
