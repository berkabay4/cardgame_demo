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
    public int maxRange = 21;

    [Header("Optional")]
    [Tooltip("Sahneye atılacak prefab (SimpleCombatant içerir). Opsiyonel.")]
    public GameObject playerPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Negatif/uygunsuz değerleri toparla
        if (maxHealth < 1) maxHealth = 1;
        if (maxRange  < 5) maxRange  = 5; // oyunun altında anlamsızsa min 5'te tut
    }
#endif
}
