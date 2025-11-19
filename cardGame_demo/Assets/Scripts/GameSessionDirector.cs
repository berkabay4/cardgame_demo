using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Map;
using System.Linq;
using System;
using SingularityGroup.HotReload;

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

    [Header("Act / Config")]
    [Tooltip("Her ACT için konfig. Combat + Mystery + Relic vs. burada tutulur.")]
    [SerializeField] private ActConfig[] actConfigs;

    [Tooltip("Act tespit edilemezse kullanılacak yedek değer.")]
    [SerializeField] private Act currentActFallback = Act.Act1;

    // RunContext'ten gelen mevcut ACT (yoksa fallback)
    private Act CurrentAct => run != null ? run.currentAct : currentActFallback;

    public MysteryData CurrentMystery { get; private set; }
    public RunContext Run => run;

    MapManager _mapManager;

    // --- Wallet erişimi ---
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

    /// <summary>Şu anki ACT için kullanılan ActConfig'i dışarıya açmak istersen bunu kullan.</summary>
    public ActConfig CurrentActConfig => ResolveCurrentActConfig();

    // === TREASURE ===
    public void StartTreasure(Map.MapNode node)
    {
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode       = node,
            nodeType      = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed          = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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
            mapNode       = node,
            nodeType      = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed          = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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
            seed          = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
        };

        // Geçerli ACT'e uygun ActConfig'i çöz
        var actConfig = ResolveCurrentActConfig();

        // 1) Node’dan Mystery ID çöz (varsayılan kural: blueprint.name)
        string mysteryId = TryGetMysteryIdFromNode(node);

        // 2) ActConfig içinden bul / yoksa weighted random
        MysteryData picked = null;
        var rng = new System.Random(run.pendingEncounter.seed);

        if (!string.IsNullOrEmpty(mysteryId) && actConfig != null)
            picked = actConfig.GetMysteryById(mysteryId);

        if (picked == null && actConfig != null)
        {
            if (!actConfig.TryGetRandomMystery(rng, out picked))
                picked = null;
        }

        if (picked == null)
        {
            Debug.LogWarning("[GSD] Uygun Mystery bulunamadı veya ActConfig yok; fallback mysteryScene yükleniyor.");
            CurrentMystery = null;
            LoadMystery(); // fallback sahne adı
            return;
        }

        CurrentMystery = picked;
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
        Debug.Log("[GSD] Starting minor encounter...");
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode       = node,
            nodeType      = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed          = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
        };
        LoadCombat();
    }

    /// <summary>Elite / mini-boss encounter başlatmak için (Elite node tıklanınca bunu çağır).</summary>
    public void StartEliteEncounter(MapNode node)
    {
        Debug.Log("[GSD] Starting ELITE encounter...");
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode       = node,
            nodeType      = node.Node.nodeType,   // Burada senin Elite node tipin gelecek
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed          = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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
        // 1) Coin ver
        if (run.pendingCoins > 0)
        {
            var w = Wallet;
            if (w != null) w.AddCoins(run.pendingCoins);
            else Debug.LogWarning("[GameSessionDirector] Wallet bulunamadı, ödül kaybedildi!");
        }

        // 2) Eğer son encounter ELITE ise, ActConfig'ten weighted relic reward dene
        if (WasLastEncounterElite())
        {
            TryGrantEliteRelicReward();
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

    // ====== ACT'e göre ActConfig çöz ======
    private ActConfig ResolveCurrentActConfig()
    {
        var act = CurrentAct;

        if (actConfigs != null && actConfigs.Length > 0)
        {
            // 1) Birebir ACT eşleşmesi
            var exact = actConfigs.FirstOrDefault(c => c != null && c.act == act);
            if (exact != null) return exact;

            // 2) Hiçbiri eşleşmezse ilk geçerli config'e düş (uyarı ver)
            var firstValid = actConfigs.FirstOrDefault(c => c != null);
            if (firstValid != null)
            {
                Debug.LogWarning($"[GSD] Act '{act}' için uygun ActConfig bulunamadı. İlk geçerli ActConfig kullanılacak: {firstValid.name}");
                return firstValid;
            }
        }

        Debug.LogWarning("[GSD] Hiçbir ActConfig atanmadı.");
        return null;
    }

    /// <summary>Şu anki ACT ve pendingEncounter.seed'e göre random mini boss definition döner (ActConfig üzerinden).</summary>
    public MiniBossDefinition GetRandomMiniBossForCurrentAct()
    {
        if (run == null || run.pendingEncounter == null)
            return null;

        var actConfig = ResolveCurrentActConfig();
        if (actConfig == null)
            return null;

        var rng = new System.Random(run.pendingEncounter.seed);
        return actConfig.GetRandomEliteEnemy(rng);
    }

    /// <summary>Son pendingEncounter'ın elite node olup olmadığını kontrol et.</summary>
    private bool WasLastEncounterElite()
    {
        if (run == null || run.pendingEncounter == null)
            return false;

        // TODO: Burayı kendi Map node type enum’una göre doldur.
        // Örneğin eğer enum'un:
        //   public enum NodeType { MinorEncounter, EliteEncounter, Treasure, ... }
        // ise:
        //
        // return run.pendingEncounter.nodeType == NodeType.EliteEncounter;
        //
        // Şimdilik default olarak false dönüyor.
        return false;
    }

    /// <summary>Elite savaş sonrası: O act'in weighted relic havuzundan rastgele relic seç.</summary>
    private void TryGrantEliteRelicReward()
    {
        var actConfig = ResolveCurrentActConfig();
        if (actConfig == null)
        {
            Debug.LogWarning("[GSD] Elite ödülü için ActConfig bulunamadı.");
            return;
        }

        var enc = run.pendingEncounter;
        // seed’in biraz farklı karışsın diye ufak xor
        var rng = new System.Random(enc.seed ^ 0xBEEF);

        if (actConfig.TryGetRandomRelic(rng, out var relic) && relic != null)
        {
            // BURADA relic'i kendi sistemine ekle:
            // Örneğin:
            // run.AddRelic(relic);
            // veya
            // relicInventory.Add(relic);
            //
            Debug.Log($"[GSD] Elite reward relic rolled: {relic.name}. Bunu RunContext / RelicInventory sistemine eklemeyi unutma.");
        }
        else
        {
            Debug.Log("[GSD] ActConfig içinde verilebilecek relic bulunamadı veya havuz boş.");
        }
    }
}
