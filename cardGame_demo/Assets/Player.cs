using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Config")]
    [SerializeField] bool dontDestroyOnLoad = true;

    [Header("Data / Refs (read-only)")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private SimpleCombatant combatant;
    [SerializeField] private HealthManager health;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private DeckOwner deckOwner;
    [SerializeField] private DeckHandle deckHandle;
    [SerializeField] private PlayerWallet wallet; // opsiyonel (varsa bağlanır)

    public PlayerData      Data       => playerData;
    public SimpleCombatant Combatant  => combatant;
    public HealthManager   Health     => health;
    public PlayerStats     Stats      => stats;
    public DeckOwner       DeckOwner  => deckOwner;
    public DeckHandle      DeckHandle => deckHandle;
    public PlayerWallet    Wallet     => wallet;

    [System.Serializable] public class PlayerDataEvent : UnityEvent<PlayerData> {}
    public PlayerDataEvent onPlayerDataChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// PlayerSpawner tarafından çağrılır. Tüm referansları ve PlayerData’yı tek noktadan set eder.
    /// </summary>
    public void AssignFromSpawn(
        PlayerData data,
        SimpleCombatant sc,
        HealthManager hm,
        PlayerStats ps,
        DeckOwner owner,
        DeckHandle handle,
        PlayerWallet w = null)
    {
        playerData = data;
        combatant  = sc;
        health     = hm;
        stats      = ps;
        deckOwner  = owner;
        deckHandle = handle;
        wallet     = w ?? PlayerWallet.Instance ?? FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);

        onPlayerDataChanged?.Invoke(playerData);
    }

    // ---- Convenience erişimler (null-safe) ----
    public int MaxHP           => health ? health.MaxHP : (playerData ? playerData.maxHealth : 1);
    public int MaxAttackRange  => stats ? stats.MaxAttackRange : (playerData ? playerData.maxAttackRange : 21);
    public int MaxDefenseRange => stats ? stats.MaxDefenseRange : (playerData ? playerData.maxDefenceRange : 21);
    public int MaxRewardRange  => playerData ? Mathf.Max(1, playerData.maxRewardRange) : 21;

    public string DisplayName  => !string.IsNullOrWhiteSpace(playerData?.playerName)
                                  ? playerData.playerName
                                  : (combatant ? combatant.name : "Player");

    // İstersen dışarıdan data güncelleme
    public void SetPlayerData(PlayerData data)
    {
        if (playerData == data) return;
        playerData = data;
        onPlayerDataChanged?.Invoke(playerData);
    }
}
