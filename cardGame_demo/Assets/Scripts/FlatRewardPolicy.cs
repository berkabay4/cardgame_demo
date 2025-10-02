using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Economy/Reward Policies/Flat Per Map", fileName = "FlatRewardPolicy")]
public class FlatRewardPolicy : ScriptableObject, IBattleRewardPolicy
{
    [Min(0)] public int baseCoins = 100;
    public int GetBaseReward(CombatDirector combatDirector) => baseCoins;
}
