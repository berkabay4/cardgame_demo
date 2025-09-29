using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerData selectedPlayer;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool parentUnderSpawner = false;   // instance'ı bu objenin altına al
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("Auto Add / Fallbacks")]
    [SerializeField] private bool autoAddSpriteRenderer  = true;
    [SerializeField] private bool autoAddPlayerStats     = true;
    [SerializeField] private bool autoAddSimpleCombatant = true; // yeni sistemde SC gerekli
    [SerializeField] private bool autoAddHealthManager   = true; // yeni: HM zorunlu

    [Header("Runtime (read-only)")]
    [SerializeField] private GameObject playerInstance;
    public GameObject      PlayerInstance => playerInstance;
    public PlayerStats     PlayerStats    { get; private set; }
    public SimpleCombatant Combatant      { get; private set; }
    public HealthManager   Health         { get; private set; }

    private void Awake()
    {
        if (!selectedPlayer)
        {
            Debug.LogError("[PlayerSpawner] Selected PlayerData atanmamış!");
            return;
        }
        SpawnOrReplace();
    }

    public void SpawnOrReplace()
    {
        // Eskiyi temizle
        if (playerInstance)
        {
            Destroy(playerInstance);
            playerInstance = null;
            PlayerStats = null;
            Combatant   = null;
            Health      = null;
        }

        // Pozisyon/rotasyon
        var pos = spawnPoint ? spawnPoint.position  : transform.position;
        var rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        // 1) Prefab ya da boş GO
        if (selectedPlayer.playerPrefab)
        {
            playerInstance = Instantiate(selectedPlayer.playerPrefab, pos, rot);
            playerInstance.name = $"Player_{selectedPlayer.playerId}";
        }
        else
        {
            playerInstance = new GameObject($"Player_{selectedPlayer.playerId}");
            playerInstance.transform.SetPositionAndRotation(pos, rot);
        }

        if (parentUnderSpawner)
            playerInstance.transform.SetParent(transform, worldPositionStays: true);

        // 2) Sprite
        var sr = playerInstance.GetComponentInChildren<SpriteRenderer>(true)
                 ?? playerInstance.GetComponent<SpriteRenderer>();
        if (!sr && autoAddSpriteRenderer) sr = playerInstance.AddComponent<SpriteRenderer>();
        if (sr && selectedPlayer.playerSprite) sr.sprite = selectedPlayer.playerSprite;

        // 3) Bileşenler: SimpleCombatant + HealthManager + (opsiyonel) PlayerStats
        var sc = playerInstance.GetComponentInChildren<SimpleCombatant>(true)
                 ?? playerInstance.GetComponent<SimpleCombatant>();
        if (!sc && autoAddSimpleCombatant) sc = playerInstance.AddComponent<SimpleCombatant>();

        var hm = playerInstance.GetComponentInChildren<HealthManager>(true)
                 ?? playerInstance.GetComponent<HealthManager>();
        if (!hm && autoAddHealthManager) hm = playerInstance.AddComponent<HealthManager>();

        var stats = playerInstance.GetComponentInChildren<PlayerStats>(true)
                    ?? playerInstance.GetComponent<PlayerStats>();
        if (!stats && autoAddPlayerStats) stats = playerInstance.AddComponent<PlayerStats>();

        Combatant = sc;
        Health    = hm;
        PlayerStats = stats;

        // 4) PlayerData → uygula (HealthManager merkezli)
        // (daha önce verdiğimiz PlayerDataApplier, HM + Stats üzerinden init ediyor)
        if (Combatant)
        {
            selectedPlayer.ApplyTo(Combatant);
            // isim
            if (!string.IsNullOrWhiteSpace(selectedPlayer.playerName))
                Combatant.name = selectedPlayer.playerName;
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] SimpleCombatant bulunamadı/eklenmedi. Sağlık/stat senkronu eksik kalabilir.");
        }

        // 5) DeckOwner / DeckHandle
        var owner = playerInstance.GetComponentInChildren<DeckOwner>(true)
                 ?? playerInstance.AddComponent<DeckOwner>();
        int seed = (playerInstance.GetInstanceID() ^ Time.frameCount);
        owner.CreateNewDeck(seed);

        var handle = playerInstance.GetComponentInChildren<DeckHandle>(true)
                  ?? playerInstance.AddComponent<DeckHandle>();
        handle.Bind(owner.Deck);

        // 6) CombatContext’e kayıt
        var ctxProv = FindAnyObjectByType<CombatContextProvider>();
        if (ctxProv && Combatant)
        {
            ctxProv.Context.RegisterPlayer(Combatant, owner.Deck);
            Debug.Log("[PlayerSpawner] Player + Deck registered to CombatContext.");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] CombatContextProvider yok veya Combatant null.");
        }

        // 7) Log
        if (Health)
            Debug.Log($"[PlayerSpawner] Applied PlayerData → name:{selectedPlayer.playerName}, HP:{Health.CurrentHP}/{Health.MaxHP}");
        else
            Debug.LogWarning("[PlayerSpawner] HealthManager yok → HP init edilmemiş olabilir.");

        // Debug.Log($"[PlayerSpawner] Player deck created. Count:{owner.Deck.Count}");

        if (dontDestroyOnLoad) DontDestroyOnLoad(playerInstance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var p = spawnPoint ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(p, 0.25f);
        UnityEditor.Handles.Label(p, "Player Spawn");
    }
#endif

    public void SetSelectedPlayer(PlayerData data, bool respawn = true)
    {
        selectedPlayer = data;
        if (respawn && data) SpawnOrReplace();
    }
}
