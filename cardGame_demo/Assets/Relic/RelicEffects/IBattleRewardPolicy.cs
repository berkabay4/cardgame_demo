public interface IBattleRewardPolicy
{
    /// <summary> Savaş bittiğinde (win) temel ödülü hesaplar. </summary>
    int GetBaseReward(GameDirector director);
}
