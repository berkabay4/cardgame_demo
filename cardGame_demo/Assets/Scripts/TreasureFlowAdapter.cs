using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class TreasureFlowAdapter : MonoBehaviour
{
    [Header("Refs (optional)")]
    [SerializeField] RunContext run;
    [SerializeField] TreasurePanelController panel;

    [Header("Database (single)")]
    [SerializeField] TreasureDatabase treasureDB; // ✅ tek giriş noktası

    [Header("Systems (optional)")]
    [SerializeField] PlayerWallet wallet;       // singleton ise boş bırakılabilir
    [SerializeField] RelicManager relicManager; // singleton ise boş bırakılabilir

    [Header("Rules")]
    [Range(0,1f)] public float relicChance = 0.5f;

    System.Random _rng;

    // --- convenience getters (singleton > cache > scene find) ---
    PlayerWallet Wallet {
        get {
            if (PlayerWallet.Instance) return PlayerWallet.Instance;
            if (!wallet) wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            return wallet;
        }
    }
    RelicManager Relics {
        get {
            if (RelicManager.Instance) return RelicManager.Instance;
            if (!relicManager) relicManager = FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
            return relicManager;
        }
    }
    TreasurePanelController Panel {
        get {
            if (!panel) panel = FindFirstObjectByType<TreasurePanelController>(FindObjectsInactive.Include);
            return panel;
        }
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m) => ResolveRefs(false);

    void Awake()  => ResolveRefs(true);
    void Start()
    {
        ResolveRefs(false);

        var p = Panel;
        if (!p) { Debug.LogError("[Treasure] Panel not found."); return; }
        if (!treasureDB) { Debug.LogError("[Treasure] TreasureDatabase not assigned."); return; }

        Act act = run ? run.currentAct : Act.Act1;

        int seed = (run != null && run.pendingEncounter != null)
            ? run.pendingEncounter.seed
            : Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(seed);

        bool giveRelic = _rng.NextDouble() < relicChance;

        if (!(giveRelic && TryOpenRelic(act)))
            OpenCoins(act); // fallback coin
    }

    void ResolveRefs(bool log)
    {
        if (!run)   run   = FindFirstObjectByType<RunContext>(FindObjectsInactive.Include);
        if (!panel) panel = FindFirstObjectByType<TreasurePanelController>(FindObjectsInactive.Include);
        if (!wallet)      wallet      = PlayerWallet.Instance ?? FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        if (!relicManager) relicManager = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);

        if (log)
            Debug.Log($"[Treasure] Refs → Run:{(run? "OK":"NULL")} Panel:{(panel? "OK":"NULL")} DB:{(treasureDB? "OK":"NULL")} Wallet:{(Wallet? "OK":"NULL")} Relics:{(Relics? "OK":"NULL")}");
    }

    bool TryOpenRelic(Act act)
    {
        var db = treasureDB?.GetRelicDB(act);
        if (!db)
        {
            Debug.LogWarning($"[Treasure] Relic DB missing for {act}. Falling back to coins.");
            return false;
        }

        if (!db.TryGetRandomRelic(_rng, out RelicDefinition def) || !def)
        {
            Debug.LogWarning($"[Treasure] Relic DB empty/null for {act}. Falling back to coins.");
            return false;
        }

        string relicName = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.name;

        // Panel API’n eski ise (relicId + relicName), aşağıdaki satır çalışır.
        Panel.Open_Relic(
            relicId:  0, // kullanılmıyor; sadece isim gösteriyoruz
            relicName: relicName,
            applyReward: () =>
            {
                var rm = Relics;
                if (rm != null) rm.Acquire(def, 1);
                else Debug.LogWarning("[Treasure] RelicManager not found, relic not granted.");
            }
        );

        Panel.onMapRequested.RemoveAllListeners();
        Panel.onMapRequested.AddListener(() => GameSessionDirector.Instance?.ReturnToMap());
        return true;
    }

    void OpenCoins(Act act)
    {
        var db = treasureDB?.GetCoinDB(act);
        int coins = db ? db.RollCoins(_rng) : 50;

        Panel.Open_Coin(
            coins,
            applyReward: () =>
            {
                var w = Wallet;
                if (w != null) w.AddCoins(coins);
                else Debug.LogWarning("[Treasure] Wallet not found, coins lost.");
            }
        );

        Panel.onMapRequested.RemoveAllListeners();
        Panel.onMapRequested.AddListener(() => GameSessionDirector.Instance?.ReturnToMap());
    }
}
