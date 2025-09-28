using UnityEngine;

public interface IEnemyTargetRangeProvider
{
    /// <summary> Faz için hedef toplam aralığı (STAND vereceği min/max). </summary>
    (int min, int max) GetRange(PhaseKind phase, int threshold);
}

// EnemyTargetRangeProvider.cs
public class EnemyTargetRangeProvider : MonoBehaviour, IEnemyTargetRangeProvider
{
    public EnemyData enemyData;

    public bool overrideAttack;
    public Vector2Int attackStandRange = new Vector2Int(14, 18);

    public bool overrideDefense;
    public Vector2Int defenseStandRange = new Vector2Int(12, 16);

    public (int min, int max) GetRange(PhaseKind phase, int threshold)
    {
        int min, max;
        if (phase == PhaseKind.Attack)
        {
            if (overrideAttack) { min = attackStandRange.x; max = attackStandRange.y; }
            else if (enemyData) { min = enemyData.attackRange.min; max = enemyData.attackRange.max; }
            else { min = 14; max = 18; }
        }
        else
        {
            if (overrideDefense) { min = defenseStandRange.x; max = defenseStandRange.y; }
            else if (enemyData) { min = enemyData.defenseRange.min; max = enemyData.defenseRange.max; }
            else { min = 12; max = 16; }
        }

        // Güvenli kıstırma: 0..threshold
        min = Mathf.Clamp(min, 0, threshold);
        max = Mathf.Clamp(max, 0, threshold);
        if (max < min) (min, max) = (max, min);

        return (min, max);
    }

    // <<< YENİ: bu düşman için hard cap (ctx.Threshold ile kıstır)
    public int GetMaxRangeCap(int ctxThreshold)
    {
        int cap = enemyData ? enemyData.maxRange : ctxThreshold;
        return Mathf.Clamp(cap, 0, ctxThreshold);
    }
}
