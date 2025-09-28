using UnityEngine;

[System.Serializable]
public struct IntRange
{
    public int min;
    public int max;

    public IntRange(int min, int max) { this.min = min; this.max = max; }

    public int RollInclusive()
    {
        if (max < min) { var t = min; min = max; max = t; }
        // Random.Range(int,int) max hariç → +1
        return Random.Range(min, max + 1);
    }

    public int Clamp(int v)
    {
        if (max < min) { var t = min; min = max; max = t; }
        return Mathf.Clamp(v, min, max);
    }

    public override string ToString() => $"[{min}..{max}]";
}

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Oyun içi görünen isim")]
    public string enemyName = "Enemy";

    [Tooltip("Benzersiz kimlik (string ya da sayısal)")]
    public string enemyId = "enemy_001";

    [Header("Visuals")]
    [Tooltip("Görsel/sprite (2D)")]
    public Sprite enemySprite;

    [Header("Stats")]
    [Min(1)] public int maxHealth = 20;

    [Tooltip("AI hedefi/olasılıkları gibi kullanabileceğin saldırı aralığı")]
    public IntRange targetattackvalueRange = new IntRange(8, 14);

    [Tooltip("AI hedefi/olasılıkları gibi kullanabileceğin savunma aralığı")]
    public IntRange targetdefensevalueRange = new IntRange(5, 12);

    [Header("Rules")]
    [Tooltip("Bu düşman için BlackJack üst sınırı (ör. 21). Savaş kurallarını düşman bazlı yapmak istersen.")]
    public int maxAttackRange = 21;
    public int maxdefenceRange = 21;
    [Header("Availability")]
    public Act[] acts = new Act[] { Act.Act1 };  // ← hangi Act’lerde görünebilir

    public bool IsForAct(Act a) => System.Array.Exists(acts, x => x == a);
    [Header("Optional")]
    [Tooltip("Sahneye atılacak prefab (SimpleCombatant içerir). Opsiyonel.")]
    public GameObject enemyPrefab;

    // --- Helpers ---
    public int RollAttack()  => targetattackvalueRange.RollInclusive();
    public int RollDefense() => targetdefensevalueRange.RollInclusive();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // negative değerleri toparla
        if (maxHealth < 1) maxHealth = 1;

        // min/max tersse çevir
        if (targetattackvalueRange.max  < targetattackvalueRange.min) { var t = targetattackvalueRange.min; targetattackvalueRange.min = targetattackvalueRange.max; targetattackvalueRange.max = t; }
        if (targetdefensevalueRange.max < targetdefensevalueRange.min) { var t = targetdefensevalueRange.min; targetdefensevalueRange.min = targetdefensevalueRange.max; targetdefensevalueRange.max = t; }
    }
#endif
}
