using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ödül akışı için context bilgisi.
/// </summary>
public class RewardContext
{
    public PlayerData playerData;
    public EconomyConfig economy;
    public int baseReward; // Savaştan gelen temel ödül (örn. 100)
    public int maxRange;   // playerData.maxRewardRange + relic etkileri vs.
    public List<int> drawnValues = new List<int>(); // çekilen kartların değerleri
    public bool isBust;    // maxRange'i aştı mı?
}

/// <summary>
/// Relic'lerin ödülü değiştirebileceği ek interface (opsiyonel).
/// </summary>
public interface IRewardRelicEffect
{
    // Ödül başlangıcında range/base değiştirmek için (örn. +range, +%base)
    int ModifyMaxRange(int currentRange, RewardContext ctx);        // default: dönen currentRange
    int ModifyBaseReward(int currentBase, RewardContext ctx);       // default: dönen currentBase

    // Kart çekmeye girmeden/çıktıktan sonra bonus ve ceza ayarları
    int ModifyBonus(int currentBonus, RewardContext ctx);           // default: dönen currentBonus
    float ModifyBustPenalty(float currentPenalty, RewardContext ctx); // default: dönen currentPenalty

    // Tamamlandığında haber ver (telemetri vs.)
    void OnRewardFinalized(int finalAmount, RewardContext ctx);
}

/// <summary>
/// Ödül hesaplama servisi. UI ile konuşur (RewardPanel), wallet'a coin ekler.
/// </summary>
public static class RewardService
{
    /// <summary>
    /// Desteden değer çek (combat destesi veya basit 1-11).
    /// </summary>
    public static int DrawValue(EconomyConfig econ, System.Random rng = null)
    {
        rng ??= new System.Random();
        if (!econ || !econ.useCombatDeckForRewards)
        {
            int min = Mathf.Max(1, econ ? econ.simpleDrawValueRange.x : 1);
            int max = Mathf.Max(min, econ ? econ.simpleDrawValueRange.y : 11);
            return rng.Next(min, max + 1);
        }

        // TODO: Combat destesi ile entegre ise buraya kendi Card/Deck çekimini bağla:
        // örn: return DeckService.Instance.Draw().GetBJValue();
        // Şimdilik fallback:
        return new System.Random().Next(1, 12);
    }

    /// <summary>
    /// O anki çekilen kartların toplamını döner.
    /// </summary>
    public static int Sum(List<int> values)
    {
        int s = 0;
        for (int i = 0; i < values.Count; i++) s += values[i];
        return s;
    }

    /// <summary>
    /// Final hesaplama.
    /// Kural:
    ///  - Eğer toplam <= maxRange: final = baseReward + bonus (bonus = toplam)
    ///  - Eğer toplam >  maxRange: final = floor(baseReward * bustPenalty), bonus=0
    /// Relic interface'i bu adımlarda devreye girebilir.
    /// </summary>
    public static int ComputeFinal(RewardContext ctx, IEnumerable<IRewardRelicEffect> rewardRelics)
    {
        if (ctx == null) return 0;
        int baseReward = Mathf.Max(0, ctx.baseReward);
        int range      = Mathf.Max(1, ctx.maxRange);

        float bustPenalty = 0.5f;
        if (ctx.economy) bustPenalty = ctx.economy.bustPenaltyFactor;

        // Relic başlangıç modifikasyonları
        if (rewardRelics != null)
        {
            foreach (var r in rewardRelics)
            {
                if (r == null) continue;
                try
                {
                    range      = Mathf.Max(1, r.ModifyMaxRange(range, ctx));
                    baseReward = Mathf.Max(0, r.ModifyBaseReward(baseReward, ctx));
                }
                catch { /* güvenli */ }
            }
        }

        int total = Sum(ctx.drawnValues);
        ctx.isBust = total > range;

        int final;
        int bonus = 0;

        if (!ctx.isBust)
        {
            bonus = total; // kural: başarıyla sınır içinde kalınca bonus = toplam
            // Relic bonus modifikasyonları
            if (rewardRelics != null)
            {
                foreach (var r in rewardRelics)
                {
                    if (r == null) continue;
                    try { bonus = Mathf.Max(0, r.ModifyBonus(bonus, ctx)); }
                    catch { }
                }
            }
            final = baseReward + bonus;
        }
        else
        {
            // Relic bust cezası modifikasyonları
            if (rewardRelics != null)
            {
                foreach (var r in rewardRelics)
                {
                    if (r == null) continue;
                    try { bustPenalty = Mathf.Clamp01(r.ModifyBustPenalty(bustPenalty, ctx)); }
                    catch { }
                }
            }
            final = Mathf.FloorToInt(baseReward * bustPenalty);
        }

        // Bildir
        if (rewardRelics != null)
        {
            foreach (var r in rewardRelics)
            {
                if (r == null) continue;
                try { r.OnRewardFinalized(final, ctx); } catch { }
            }
        }

        return Mathf.Max(0, final);
    }
}
