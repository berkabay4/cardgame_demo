using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Economy/Config", fileName = "EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Coin & Ödül Ayarları")]
    [Tooltip("Bust (sınırı aşma) durumunda temel ödüle uygulanacak çarpan. Örn. 0.5 => ödül yarıya düşer.")]
    [Range(0f, 1f)] public float bustPenaltyFactor = 0.5f;

    [Tooltip("Ödül mini oyununda çekilebilecek maksimum kart sayısı (0 = sınırsız).")]
    [Min(0)] public int maxDrawCount = 0;

    [Tooltip("Kart çekişi aynı desteden mi, yoksa sade tamsayı 1-11 mi? Eğer false ise 1-11 arası değer üretir.")]
    public bool useCombatDeckForRewards = false;

    [Tooltip("Sade modda üretilecek kart değer aralığı (örn. 1-11).")]
    public Vector2Int simpleDrawValueRange = new Vector2Int(1, 11);

    [Tooltip("Ödül ekranı defaultta açık geldiğinde otomatik çekim kapalı mı?")]
    public bool autoDraw = false;
}
