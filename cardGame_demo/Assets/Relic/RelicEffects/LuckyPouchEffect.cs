using UnityEngine;

/// <summary>
/// Her stack için reward range +2, bonusu %10 artır.
/// </summary>
[System.Serializable]
public class LuckyPouchEffect : IRewardRelicEffect
{
    [Min(0)] public int rangePerStack = 2;
    [Range(0f, 2f)] public float bonusMultiplier = 1.10f;

    int GetStacks(RelicRuntime r) => Mathf.Max(1, r != null ? r.stacks : 1);

    public int ModifyMaxRange(int currentRange, RewardContext ctx)
    {
        // RelicRuntime erişimin yoksa bu sınıfı RelicRuntime alan bir wrapper içinde kullanabilirsin.
        // Basit örnek: sabit +2
        return currentRange + rangePerStack;
    }

    public int ModifyBaseReward(int currentBase, RewardContext ctx) => currentBase;

    public int ModifyBonus(int currentBonus, RewardContext ctx)
    {
        return Mathf.RoundToInt(currentBonus * bonusMultiplier);
    }

    public float ModifyBustPenalty(float currentPenalty, RewardContext ctx) => currentPenalty;

    public void OnRewardFinalized(int finalAmount, RewardContext ctx) { }
}
    