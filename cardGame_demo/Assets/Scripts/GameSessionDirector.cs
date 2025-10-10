using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Map;
using System.Linq;

public class GameSessionDirector : MonoBehaviour
{
    public static GameSessionDirector Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] string mapScene       = "MapScene";
    [SerializeField] string combatScene    = "CombatScene";
    [SerializeField] string treasureScene  = "TreasureScene";
    [SerializeField] string restSiteScene  = "RestSiteScene";
    [SerializeField] string mysteryScene   = "MysteryScene"; // Fallback: CurrentMystery boşsa kullanılır

    [Header("Refs")]
    [SerializeField] RunContext run;
    [Tooltip("Opsiyonel. Atanmazsa otomatik bulunur.")]
    private PlayerWallet wallet;

    [SerializeField] int baseMinorCoins = 50;

    // === Mystery (Data-Driven) ===
    [Header("Mystery (Data-Driven)")]
    [SerializeField] private ActMysteryDatabase mysteryDb;   // Inspector’dan ver
    public MysteryData CurrentMystery { get; private set; }
    public RunContext Run => run;

    MapManager _mapManager;

    // --- Wallet erişimi (önce Singleton, sonra sahnede ara, sonra serialized fallback) ---
    PlayerWallet Wallet
    {
        get
        {
            if (PlayerWallet.Instance != null) return PlayerWallet.Instance;
            if (wallet == null)
                wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            return wallet;
        }
    }

    // === TREASURE ===
    public void StartTreasure(Map.MapNode node)
    {
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode = node,
            nodeType = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed = Random.Range(int.MinValue, int.MaxValue),
        };
        LoadTreasure();
    }

    async void LoadTreasure()
    {
        var op = SceneManager.LoadSceneAsync(treasureScene, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }

    /// <summary>Treasure sahnesi Claim edildiğinde buraya gelinir.</summary>
    public void ReportTreasureFinished(int coinsGranted)
    {
        var w = Wallet;
        if (coinsGranted > 0 && w != null) w.AddCoins(coinsGranted);

        AdvanceMapProgressAfterWin();
        ReturnToMap();
    }

    // === REST SITE ===
    public void StartRestSite(Map.MapNode node)
    {
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode = node,
            nodeType = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed = Random.Range(int.MinValue, int.MaxValue),
        };
        LoadRestSite();
    }

    async void LoadRestSite()
    {
        var op = SceneManager.LoadSceneAsync(restSiteScene, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }

    /// <summary>RestSite sahnesi tamamlandığında çağır.</summary>
    public void ReportRestSiteFinished(int coinsGranted = 0)
    {
        var w = Wallet;
        if (coinsGranted > 0 && w != null) w.AddCoins(coinsGranted);

        AdvanceMapProgressAfterWin();
        ReturnToMap();
    }

    // === MYSTERY ===
    public enum MysteryOutcome
    {
        None,
        Coins,
        StartCombat,
        StartTreasure,
        Nothing
    }

    public void StartMystery(Map.MapNode node)
    {
        // Encounter hazırla
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode       = node,
            nodeType      = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed          = Random.Range(int.MinValue, int.MaxValue),
        };

        // 1) Node’dan Mystery ID çöz (varsayılan kural: blueprint.name)
        string mysteryId = TryGetMysteryIdFromNode(node);

        // 2) ActMysteryDatabase'ten bul / yoksa weighted random (Entry.weight)
        MysteryData picked = null;
        var rng = new System.Random(run.pendingEncounter.seed);

        if (!string.IsNullOrEmpty(mysteryId) && mysteryDb != null)
            picked = mysteryDb.GetById(mysteryId);

        if (picked == null && mysteryDb != null)
        {
            if (!mysteryDb.TryGetRandomMystery(rng, out picked))
                picked = null;
        }

        if (picked == null)
        {
            Debug.LogWarning("[GSD] Mystery bulunamadı; fallback mysteryScene yükleniyor.");
            CurrentMystery = null;
            LoadMystery(); // fallback sahne adı
            return;
        }

        CurrentMystery = picked;
        // (opsiyonel) run.pendingEncounter.mysteryId = picked.id;
        LoadMysteryScene();
    }

    // Varsayılan eşleştirme: Blueprint.name == MysteryData.id
    string TryGetMysteryIdFromNode(Map.MapNode node)
        => node?.Blueprint ? node.Blueprint.name : null;
    /// <summary>GERİ UYUMLULUK: Eğer CurrentMystery yoksa eski 'mysteryScene' stringini yükler.</summary>
    async void LoadMystery()
    {
        // Eski davranış (sahne adı sabit)
        var sceneToLoad = !string.IsNullOrEmpty(mysteryScene) ? mysteryScene : "MysteryScene";
        var op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }

    /// <summary>Yeni davranış: CurrentMystery.sceneName’i yükler; boşsa fallback’e döner.</summary>
    async void LoadMysteryScene()
    {
        if (CurrentMystery == null || string.IsNullOrEmpty(CurrentMystery.sceneName))
        {
            Debug.LogWarning("[GSD] CurrentMystery veya sceneName boş, fallback 'mysteryScene' kullanılacak.");
            LoadMystery();
            return;
        }

        var op = SceneManager.LoadSceneAsync(CurrentMystery.sceneName, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }

    /// <summary>Mystery sahnesi sonucunu raporla.</summary>
    public void ReportMysteryFinished(MysteryOutcome outcome, int coinsGranted = 0)
    {
        switch (outcome)
        {
            case MysteryOutcome.StartCombat:
                LoadCombat();
                return;

            case MysteryOutcome.StartTreasure:
                LoadTreasure();
                return;

            case MysteryOutcome.Coins:
                var w = Wallet;
                if (coinsGranted > 0 && w != null) w.AddCoins(coinsGranted);
                break;

            case MysteryOutcome.Nothing:
            case MysteryOutcome.None:
            default:
                break;
        }

        // Combat/Treasure’a dallanmadıysa: nodu tamamla ve haritaya dön
        AdvanceMapProgressAfterWin();
        ReturnToMap();
    }

    // === LIFECYCLE ===
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveSceneRefs();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSceneRefs();
    }

    void ResolveSceneRefs()
    {
        _mapManager = FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
        if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        // if (!run) run = Resources.Load<RunContext>("RunContext");
    }

    // === MAP → COMBAT ===
    public void StartMinorEncounter(MapNode node)
    {
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode = node,
            nodeType = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed = Random.Range(int.MinValue, int.MaxValue),
        };
        LoadCombat();
    }

    async void LoadCombat()
    {
        var op = SceneManager.LoadSceneAsync(combatScene, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
        // CombatFlowAdapter sahnede GameDirector’a bağlanacak
    }

    // === COMBAT → RESULT ===
    public void ReportCombatFinished(bool playerWon, int coins)
    {
        run.lastCombatResult = new RunContext.CombatResult { playerWon = playerWon, coins = coins };

        if (playerWon)
        {
            run.pendingCoins = coins > 0 ? coins : baseMinorCoins;
            AcceptRewardAndReturnToMap();
        }
        else
        {
            ReturnToMap();
        }
    }

    public void AcceptRewardAndReturnToMap()
    {
        if (run.pendingCoins > 0)
        {
            var w = Wallet;
            if (w != null) w.AddCoins(run.pendingCoins);
            else Debug.LogWarning("[GameSessionDirector] Wallet bulunamadı, ödül kaybedildi!");
        }

        AdvanceMapProgressAfterWin();
        run.pendingCoins = 0;
        ReturnToMap();
    }

    public void ReturnToMap() => OpenMapFromReward();
    bool _returningToMap;

    public void OpenMapFromReward()
    {
        if (_returningToMap) { Debug.Log("[GSD] OpenMapFromReward ignored (already returning)."); return; }
        _returningToMap = true;

        Debug.Log("[GSD] OpenMapFromReward requested.");
        StartCoroutine(ReturnToMapCo());

        System.Collections.IEnumerator ReturnToMapCo()
        {
            Debug.Log($"[GSD] Loading scene: {mapScene}");
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(mapScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            Debug.Log($"[GSD] Scene loaded: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            ResolveSceneRefs();

            var mm = _mapManager;
            if (mm != null && mm.view != null)
            {
                Debug.Log("[GSD] Redrawing map...");
                mm.view.ShowMap(mm.CurrentMap);
            }
            else
            {
                Debug.LogWarning("[GSD] MapManager or View not found after scene load.");
            }

            _returningToMap = false;
        }
    }

    void AdvanceMapProgressAfterWin()
    {
        if (_mapManager == null || _mapManager.CurrentMap == null || run.pendingEncounter == null) return;

        var p = run.pendingEncounter.mapNode.Node.point;
        var path = _mapManager.CurrentMap.path;

        if (path.Count == 0 || path[^1] != p)
            path.Add(p);

        // Görselle güncelle
        var node = _mapManager.view?.MapNodes?.FirstOrDefault(n => n.Node.point == p);
        if (node) node.SetState(Map.NodeStates.Visited);

        _mapManager.view?.SetAttainableNodes();
        _mapManager.view?.SetLineColors();
    }
}
