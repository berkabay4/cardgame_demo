using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Map;
using System.Linq;

public class GameSessionDirector : MonoBehaviour
{
    public static GameSessionDirector Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] string mapScene    = "MapScene";
    [SerializeField] string combatScene = "CombatScene";
    [SerializeField] string treasureScene = "TreasureScene";  // ← yeni sahne adı

    [SerializeField] int baseTreasureCoins = 80;              // treasure baz ödül
    [Header("Refs")]
    [SerializeField] RunContext run;
    [Tooltip("Opsiyonel. Atanmazsa otomatik bulunur.")]
    private PlayerWallet wallet;

    [SerializeField] int baseMinorCoins = 50;

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
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(treasureScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
    }

    /// <summary>Treasure sahnesi Claim edildiğinde buraya gelinir.</summary>
    public void ReportTreasureFinished(int coinsGranted)
    {
        // cüzdana yaz
        var w = Wallet;
        if (coinsGranted > 0 && w != null) w.AddCoins(coinsGranted);

        // path zaten selection anında eklenmişse dokunma; yoksa güvence:
        AdvanceMapProgressAfterWin();

        // map'e dön
        ReturnToMap();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // İlk çözüm
        ResolveSceneRefs();

        // Sahne değişince referansları tazele
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
        // MapManager
        _mapManager = FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
        // Wallet
        if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        // RunContext boşsa kaynakla (Resources/Inspector vs.)
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
            // İstersen burada Reward UI aç; şimdilik direkt kabul:
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
            var w = Wallet; // otomatik çözülmüş wallet
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
