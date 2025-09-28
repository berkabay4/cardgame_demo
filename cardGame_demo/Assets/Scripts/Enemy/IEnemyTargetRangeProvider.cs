using UnityEngine;

public interface IEnemyTargetRangeProvider
{
    /// <summary> Faz için hedef toplam aralığı (AI'nin Stand vereceği min/max).</summary>
    (int min, int max) GetRange(PhaseKind phase, int threshold);
}
// EnemyTargetRangeProvider.cs

public class EnemyTargetRangeProvider : MonoBehaviour, IEnemyTargetRangeProvider
{
    [Header("Data")]
    public EnemyData enemyData;

    [Header("Overrides")]
    public bool overrideAttack;
    public Vector2Int attackStandRange = new Vector2Int(14, 18);

    public bool overrideDefense;
    public Vector2Int defenseStandRange = new Vector2Int(12, 16);

    /// <summary>
    /// Faz için hedef (min,max) aralığı. Sonuç, hem sahne eşiği (threshold) hem de
    /// düşmanın faza özel "hard cap" değeri ile kıstırılır.
    /// </summary>
    public (int min, int max) GetRange(PhaseKind phase, int threshold)
    {
        int min, max;

        if (phase == PhaseKind.Attack)
        {
            if (overrideAttack)
            {
                min = attackStandRange.x; max = attackStandRange.y;
            }
            else if (enemyData)
            {
                min = enemyData.targetattackvalueRange.min;
                max = enemyData.targetattackvalueRange.max;
            }
            else
            {
                min = 14; max = 18;
            }
        }
        else // Defense
        {
            if (overrideDefense)
            {
                min = defenseStandRange.x; max = defenseStandRange.y;
            }
            else if (enemyData)
            {
                min = enemyData.targetdefensevalueRange.min;
                max = enemyData.targetdefensevalueRange.max;
            }
            else
            {
                min = 12; max = 16;
            }
        }

        // Fazın hard cap'ini hesapla: EnemyData'daki max faz eşiği, ctx threshold'ünün üstüne çıkamaz
        int phaseCap = GetMaxRangeCap(phase, threshold);

        // Güvenli kıstırma: 0..phaseCap (phaseCap zaten 0..threshold aralığında)
        min = Mathf.Clamp(min, 0, phaseCap);
        max = Mathf.Clamp(max, 0, phaseCap);
        if (max < min) (min, max) = (max, min);

        return (min, max);
    }

    /// <summary>
    /// Bu düşmanın faza özel hard cap değeri (ATK/DEF ayrı).
    /// ctxThreshold ile kıstırılır ki oyun sahnesinin eşik üstüne çıkmasın.
    /// </summary>
    public int GetMaxRangeCap(PhaseKind phase, int ctxThreshold)
    {
        int cap;
        if (enemyData)
        {
            cap = (phase == PhaseKind.Attack)
                ? enemyData.maxAttackRange
                : enemyData.maxdefenceRange;
        }
        else
        {
            cap = ctxThreshold; // data yoksa sahne eşiği ile sınırla
        }

        // Faz cap'i, sahne eşiğini (ctxThreshold) aşmasın
        return Mathf.Clamp(cap, 0, ctxThreshold);
    }
}