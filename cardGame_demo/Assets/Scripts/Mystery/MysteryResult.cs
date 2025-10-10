
public struct MysteryResult
{
    public GameSessionDirector.MysteryOutcome outcome;
    public int coins;

    public MysteryResult(GameSessionDirector.MysteryOutcome outcome, int coins = 0)
    {
        this.outcome = outcome;
        this.coins = coins;
    }
}
