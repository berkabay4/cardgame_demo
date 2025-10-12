using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Map;
using System.Linq;
using System;

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
    private Act CurrentAct => run != null ? run.currentAct : currentActFallback;
    // === Mystery (Data-Driven) ===
    [Header("Mystery (Data-Driven)")]
    [Tooltip("Mevcut ACT'e göre buradaki listeden uygun DB seçilir.")]
    [SerializeField] private ActMysteryDatabase[] mysteryDbs;

    [Tooltip("Geri uyumluluk için tekil DB. Dizi boş/uygun yoksa bundan okunur.")]
    [SerializeField] private ActMysteryDatabase mysteryDb;

    [Tooltip("Act tespit edilemezse kullanılacak yedek değer.")]
    [SerializeField] private Act currentActFallback = Act.Act1;

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

    // === TREASURE ===
    public void StartTreasure(Map.MapNode node)
    {
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode = node,
            nodeType = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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

        // Geçerli ACT'e uygun DB'yi çöz
        var db = ResolveActiveMysteryDb();

        // 1) Node’dan Mystery ID çöz (varsayılan kural: blueprint.name)
        string mysteryId = TryGetMysteryIdFromNode(node);

        // 2) DB’den bul / yoksa weighted random
        MysteryData picked = null;
        var rng = new System.Random(run.pendingEncounter.seed);

        if (!string.IsNullOrEmpty(mysteryId) && db != null)
            picked = db.GetById(mysteryId);

        if (picked == null && db != null)
        {
            if (!db.TryGetRandomMystery(rng, out picked))
                picked = null;
        }

        if (picked == null)
        {
            Debug.LogWarning("[GSD] Uygun Mystery bulunamadı veya DB yok; fallback mysteryScene yükleniyor.");
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
        run.pendingEncounter = new RunContext.EncounterData {
            mapNode = node,
            nodeType = node.Node.nodeType,
            blueprintName = node.Blueprint ? node.Blueprint.name : null,
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
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

    // ====== YENİ KISIM: ACT çöz ve uygun DB'yi seç ======

    /// <summary>Mevcut ACT'i tespit eder: Önce MapManager.CurrentMap.act, sonra RunContext (act/currentAct), en son fallback.</summary>
    private Act ResolveCurrentAct()
    {
        // 1) MapManager.CurrentMap.act (eğer varsa)
        try
        {
            var curMap = _mapManager?.CurrentMap;
            if (curMap != null)
            {
                var mapType = curMap.GetType();
                var actProp = mapType.GetProperty("act") ?? mapType.GetProperty("Act");
                if (actProp != null && actProp.PropertyType == typeof(Act))
                    return (Act)actProp.GetValue(curMap);
            }
        }
        catch { /* yoksay */ }

        // 2) RunContext'ten (act veya currentAct alan/prop)
        try
        {
            if (run != null)
            {
                var t = run.GetType();
                var p = t.GetProperty("act") ?? t.GetProperty("currentAct") ?? t.GetProperty("Act") ?? t.GetProperty("CurrentAct");
                if (p != null && p.PropertyType == typeof(Act))
                    return (Act)p.GetValue(run);

                var f = t.GetField("act") ?? t.GetField("currentAct") ?? t.GetField("Act") ?? t.GetField("CurrentAct");
                if (f != null && f.FieldType == typeof(Act))
                    return (Act)f.GetValue(run);
            }
        }
        catch { /* yoksay */ }

        // 3) Fallback
        return currentActFallback;
    }

    /// <summary>mysteryDbs içinden ACT'e uyanı döndürür; bulunamazsa tekil mysteryDb'ye düşer.</summary>
    private ActMysteryDatabase ResolveActiveMysteryDb()
    {
        var act = CurrentAct;

        if (mysteryDbs != null && mysteryDbs.Length > 0)
        {
            // Birebir ACT eşleşmesi
            var exact = mysteryDbs.FirstOrDefault(db => db != null && db.act == act);
            if (exact != null) return exact;

            // Hiçbiri eşleşmezse ilk geçerli DB'ye düş (uyarı ver)
            var firstValid = mysteryDbs.FirstOrDefault(db => db != null);
            if (firstValid != null)
            {
                Debug.LogWarning($"[GSD] Act '{act}' için uygun ActMysteryDatabase bulunamadı. İlk geçerli DB kullanılacak: {firstValid.name}");
                return firstValid;
            }
        }

        // Dizi yoksa/boşsa eski tekil alanı kullan (geri uyumluluk)
        if (mysteryDb != null)
        {
            Debug.LogWarning("[GSD] mysteryDbs boş; geri uyumluluk için tekil 'mysteryDb' kullanılacak.");
            return mysteryDb;
        }

        Debug.LogWarning("[GSD] Hiçbir ActMysteryDatabase atanmadı.");
        return null;
    }
}
