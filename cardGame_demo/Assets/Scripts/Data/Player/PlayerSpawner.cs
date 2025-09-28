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
    [SerializeField] private bool autoAddSpriteRenderer   = true;
    [SerializeField] private bool autoAddPlayerStats      = true;
    [SerializeField] private bool autoAddSimpleCombatant  = false; // İstersen player’a SC ekle

    [Header("Runtime (read-only)")]
    [SerializeField] private GameObject playerInstance;
    public GameObject PlayerInstance => playerInstance;
    public PlayerStats PlayerStats   { get; private set; }
    public SimpleCombatant Combatant { get; private set; }

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
            Combatant = null;
        }

        // Pozisyon/rotasyon hesabı
        var pos = spawnPoint ? spawnPoint.position : transform.position;
        var rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        // 1) Prefab varsa instantiat et, yoksa boş GO oluştur
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

        if (parentUnderSpawner) playerInstance.transform.SetParent(transform, worldPositionStays: true);

        // 2) Sprite
        var sr = playerInstance.GetComponentInChildren<SpriteRenderer>(true) 
                 ?? playerInstance.GetComponent<SpriteRenderer>();
        if (!sr && autoAddSpriteRenderer) sr = playerInstance.AddComponent<SpriteRenderer>();
        if (sr && selectedPlayer.playerSprite) sr.sprite = selectedPlayer.playerSprite;

        // 3) PlayerStats
        var stats = playerInstance.GetComponentInChildren<PlayerStats>(true) 
                    ?? playerInstance.GetComponent<PlayerStats>();
        if (!stats && autoAddPlayerStats) stats = playerInstance.AddComponent<PlayerStats>();

        if (stats)
        {
            stats.InitFrom(selectedPlayer);                     // MaxHealth, CurrentHealth, MaxRange
            PlayerStats = stats;
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] PlayerStats bulunamadı (ve eklenmedi). Fallback olarak SimpleCombatant alanlarına yazılacak.");
        }

        // 4) SimpleCombatant
        var sc = playerInstance.GetComponentInChildren<SimpleCombatant>(true) 
                 ?? playerInstance.GetComponent<SimpleCombatant>();
        if (!sc && autoAddSimpleCombatant) sc = playerInstance.AddComponent<SimpleCombatant>();
        Combatant = sc;

        // 5) PlayerData → SimpleCombatant’a uygula
        // Tercih 1: PlayerStats varsa ondan senkronla
        if (Combatant && PlayerStats)
        {
            Combatant.ApplyFromStats(PlayerStats, refillToMax: true);
            Combatant.name = string.IsNullOrWhiteSpace(selectedPlayer.playerName) 
                             ? Combatant.name : selectedPlayer.playerName;
        }
        // Tercih 2: Stats yoksa direkt Combatant alanlarına yaz (fallback)
        else if (Combatant && !PlayerStats)
        {
            int hp = Mathf.Max(1, selectedPlayer.maxHealth);
            Combatant.maxHP = hp;               // SC alanı (senin sınıfındaki public field)
            Combatant.CurrentHP = hp;
            Combatant.name = string.IsNullOrWhiteSpace(selectedPlayer.playerName) 
                             ? Combatant.name : selectedPlayer.playerName;
        }

        var owner = playerInstance.GetComponentInChildren<DeckOwner>(true)
                ?? playerInstance.AddComponent<DeckOwner>();
        int seed = (playerInstance.GetInstanceID() ^ Time.frameCount);
        owner.CreateNewDeck(seed);

        // (opsiyonel) DeckHandle
        var handle = playerInstance.GetComponentInChildren<DeckHandle>(true)
                ?? playerInstance.AddComponent<DeckHandle>();
        handle.Bind(owner.Deck);

        // Context'e kaydet
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

        Debug.Log($"[PlayerSpawner] Player deck created. Count:{owner.Deck.Count}");
        // 7) Log
        if (PlayerStats)
            Debug.Log($"[PlayerSpawner] Applied PlayerData → name:{selectedPlayer.playerName}, HP:{PlayerStats.CurrentHealth}/{PlayerStats.MaxHealth}, MaxRange:{PlayerStats.MaxRange}");
        else if (Combatant)
            Debug.Log($"[PlayerSpawner] Applied PlayerData (fallback) → name:{selectedPlayer.playerName}, HP:{Combatant.CurrentHP}/{Combatant.MaxHP}");
        else
            Debug.LogWarning("[PlayerSpawner] SimpleCombatant da yok → yalnızca görsel/isim uygulanmış olabilir.");

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
