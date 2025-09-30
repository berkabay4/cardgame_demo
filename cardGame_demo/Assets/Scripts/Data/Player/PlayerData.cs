using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Player/Player Data", fileName = "PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Oyun içi görünen isim")]
    public string playerName = "Player";

    [Tooltip("Benzersiz kimlik (string ya da sayısal)")]
    public string playerId = "player_001";

    [Header("Visuals")]
    [Tooltip("Görsel/sprite (2D)")]
    public Sprite playerSprite;

    [Header("Stats")]
    [Min(1)] public int maxHealth = 20;

    [Header("Rules")]
    [Tooltip("BlackJack üst sınırı (default 21)")]
    public int maxAttackRange = 21;
    public int maxDefenceRange = 21;

    [Header("Rewards / Economy")]
    [Tooltip("Ödül kart çekme mini-oyunu için üst sınır (örn. 21). Başarıyla <= sınırda kalınırsa bonus = toplam.")]
    [Min(1)] public int maxRewardRange = 21;

    [Tooltip("Oyuncunun başlangıç coin miktarı (opsiyonel).")]
    [Min(0)] public int startingCoins = 0;

    [Header("Optional")]
    [Tooltip("Sahneye atılacak prefab (SimpleCombatant içerir). Opsiyonel.")]
    public GameObject playerPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1) maxHealth = 1;
        if (maxRewardRange < 1) maxRewardRange = 1;
        if (startingCoins < 0) startingCoins = 0;
    }
#endif
}
