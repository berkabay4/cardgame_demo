using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(3);
    [Tooltip("Varsayılan prefab (EnemyData.enemyPrefab yoksa kullanılır)")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform parentForSpawned;

    [Header("Act & Source")]
    [SerializeField] private Act currentAct = Act.Act1;        // bulunduğun act
    [SerializeField] private EnemyDatabase database;           // tüm düşman dataları

    [Header("Options")]
    [SerializeField] private bool destroyExistingOnPoint = true;
    [SerializeField] private bool pickRandom = true;           // aynı act’te birden çok data varsa rastgele seç
    [SerializeField] private bool registerToContext = true;    // true: son spawn edilen düşman CombatContext.Enemy olur

    [Header("Test (Inspector)")]
    [SerializeField, Min(0)] private int testSpawnCount = 1;
    [SerializeField] private bool spawnOnToggle = false;
    [SerializeField] private bool autoResetToggle = true;
    [SerializeField] private bool useDelayForTest = false;
    [SerializeField, Min(0f)] private float testDelayBetween = 0.2f;

    private bool _prevSpawnOnToggle;

    // ---- Events ----
    [Header("Events")]
    public UnityEvent onEnemiesSpawned;               // tüm spawnlar bitti
    public UnityEvent<GameObject> onEnemySpawned;     // her yeni düşman için
    public static event System.Action EnemiesSpawned; // global (isteğe bağlı)
    private struct PendingEnemy
    {
        public SimpleCombatant sc;
        public IDeckService deck;
        public int atkMax;
        public int defMax;
    }
    // private readonly List<(SimpleCombatant sc, IDeckService deck)> _pendingCtxRegs = new();
    private readonly List<PendingEnemy> _pendingCtxRegs = new();
    // === Public API ===
    public List<GameObject> SpawnCount(int count)
    {
        var spawned = new List<GameObject>();

        if ((!database || database.all.Count == 0) && !enemyPrefab)
        {
            Debug.LogWarning("[EnemySpawner] No EnemyDatabase / Prefab.");
            return spawned;
        }
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No spawn points.");
            return spawned;
        }

        int n = Mathf.Clamp(count, 0, spawnPoints.Count);

        for (int i = 0; i < n; i++)
        {
            var p = spawnPoints[i];
            if (!p) continue;

            if (destroyExistingOnPoint) DestroyChildrenOf(p);

            var data = PickEnemyDataForAct();
            var go = SpawnFromData(p, data);
            if (!go) continue;

            SetupEnemyRuntime(go, data);   // <— DECK + CONTEXT burada
            spawned.Add(go);
            onEnemySpawned?.Invoke(go);
        }

        // kullanılmayan noktaları boşalt
        if (destroyExistingOnPoint)
        {
            for (int i = n; i < spawnPoints.Count; i++)
                DestroyChildrenOf(spawnPoints[i]);
        }

        onEnemiesSpawned?.Invoke();
        EnemiesSpawned?.Invoke();
        return spawned;
    }
    void OnEnable()
    {
        CombatDirector.ContextReady += FlushPendingContextRegs;
    }
    void OnDisable()
    {
        CombatDirector.ContextReady -= FlushPendingContextRegs;
    }
    void FlushPendingContextRegs()
    {
        var ctx = CombatDirector.Instance ? CombatDirector.Instance.Ctx : null;
        if (ctx == null || _pendingCtxRegs.Count == 0) return;

        foreach (var p in _pendingCtxRegs)
        {
            if (p.sc && p.deck != null)
            {
                ctx.RegisterEnemy(p.sc, p.deck);
                ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Attack,  p.atkMax);
                ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Defense, p.defMax);

                // Debug.Log($"[EnemySpawner] Pending enemy registered + thresholds set → {p.sc.name} (ATK:{p.atkMax}, DEF:{p.defMax})");
            }
        }
        _pendingCtxRegs.Clear();
    }

    public IEnumerator SpawnCountWithDelay(int count, float delayBetween = 0.2f)
    {
        if ((!database || database.all.Count == 0) && !enemyPrefab)
        {
            Debug.LogWarning("[EnemySpawner] No EnemyDatabase / Prefab.");
            yield break;
        }
        if (spawnPoints.Count == 0) yield break;

        int n = Mathf.Clamp(count, 0, spawnPoints.Count);

        for (int i = 0; i < n; i++)
        {
            var p = spawnPoints[i];
            if (!p) continue;

            if (destroyExistingOnPoint) DestroyChildrenOf(p);

            var data = PickEnemyDataForAct();
            var go = SpawnFromData(p, data);
            if (go)
            {
                SetupEnemyRuntime(go, data);   // <— gecikmeli akışta da kur
                onEnemySpawned?.Invoke(go);
            }

            if (delayBetween > 0f) yield return new WaitForSeconds(delayBetween);
        }

        if (destroyExistingOnPoint)
        {
            for (int i = n; i < spawnPoints.Count; i++)
                DestroyChildrenOf(spawnPoints[i]);
        }

        onEnemiesSpawned?.Invoke();
        EnemiesSpawned?.Invoke();
    }

    // === Internals ===
    EnemyData PickEnemyDataForAct()
    {
        if (!database || database.all.Count == 0) return null;

        var pool = new List<EnemyData>();
        foreach (var d in database.all)
            if (d && d.IsForAct(currentAct))
                pool.Add(d);

        if (pool.Count == 0) return null;
        return pickRandom ? pool[Random.Range(0, pool.Count)] : pool[0];
    }

    GameObject SpawnFromData(Transform point, EnemyData data)
    {
        GameObject prefab = data && data.enemyPrefab ? data.enemyPrefab : enemyPrefab;
        if (!prefab)
        {
            Debug.LogWarning("[EnemySpawner] No prefab to spawn.");
            return null;
        }

        var go = Instantiate(prefab, point.position, point.rotation, parentForSpawned ? parentForSpawned : null);

        // spawn meta
        var meta = go.GetComponent<EnemySpawnMeta>() ?? go.AddComponent<EnemySpawnMeta>();
        meta.source = this;
        meta.spawnPoint = point;
        meta.spawnIndex = spawnPoints.IndexOf(point);

        // data uygula (SimpleCombatant varsayımı)
        var sc   = go.GetComponent<SimpleCombatant>();
        var prov = go.GetComponent<EnemyTargetRangeProvider>() ?? go.AddComponent<EnemyTargetRangeProvider>();
        prov.enemyData = data; // Attack/Defense stand aralıkları buradan okunacak
        if (data && sc)
        {
            EnemyDataApplier.ApplyTo(data, sc);
        }

        return go;
    }

    /// <summary>Spawn edilen düşman üstünde runtime kurulum: Deck, Handle, Context kaydı.</summary>
    void SetupEnemyRuntime(GameObject go, EnemyData data)
    {
        if (!go) return;

        // ---- 1) DeckOwner/DeckHandle hazırla ----
        var owner  = go.GetComponentInChildren<DeckOwner>(true) ?? go.AddComponent<DeckOwner>();
        var handle = go.GetComponentInChildren<DeckHandle>(true) ?? go.AddComponent<DeckHandle>();

        // ---- 2) CombatantDeck varsa ondan kur, yoksa fallback kur ----
        IDeckService deck = null;
        var combatantDeck = go.GetComponentInChildren<CombatantDeck>(true);
        if (combatantDeck != null)
        {
            deck = combatantDeck.BuildDeck();            // DeckData → cards → SetInitialCards+Shuffle
        }
        else
        {
            Debug.LogError("[EnemySpawner] No CombatantDeck found on enemy prefab. Please add one for proper deck setup.");
            // // Fallback: 52 + 1 Joker
            // var d = new DeckService();
            // var cards = CreateDefault52PlusJoker();
            // d.SetInitialCards(cards, takeSnapshot: true);
            // d.Shuffle();
            // deck = d;
        }

        // DeckOwner'a ver ve handle'ı bağla
        owner.SetDeck(deck as DeckService ?? new DeckService());   // DeckService tipindeyse direkt set
        handle.Bind(owner.Deck);

        // ---- 3) Context kaydı + threshold ----
        var sc  = go.GetComponentInChildren<SimpleCombatant>(true);
        var ctx = CombatDirector.Instance ? CombatDirector.Instance.Ctx : null;

        // EnemyData’dan faz eşikleri (yoksa global fallback)
        int atkMax = (data != null) ? Mathf.Max(5, data.maxAttackRange)  : ((ctx != null) ? ctx.Threshold : 21);
        int defMax = (data != null) ? Mathf.Max(5, data.maxdefenceRange) : ((ctx != null) ? ctx.Threshold : 21);

        if (registerToContext && ctx != null && sc != null)
        {
            ctx.RegisterEnemy(sc, owner.Deck);
            ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Attack,  atkMax);
            ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Defense, defMax);
            Debug.Log($"[EnemySpawner] Registered '{sc.name}' → Deck={owner.Deck.Count}, ATK:{atkMax}, DEF:{defMax}");
        }
        else
        {
            if (sc != null)
            {
                _pendingCtxRegs.Add(new PendingEnemy { sc = sc, deck = owner.Deck, atkMax = atkMax, defMax = defMax });
                Debug.LogWarning($"[EnemySpawner] Context not ready — queued '{sc.name}'. Deck={owner.Deck.Count}, ATK:{atkMax}, DEF:{defMax}");
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] SimpleCombatant missing — cannot register.");
            }
        }
    }

    // İstersen dosyanın altına ekle (fallback için)
    List<Card> CreateDefault52PlusJoker()
    {
        var list = new List<Card>(53);
        string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades" };
        foreach (var s in suits)
        {
            for (int v = 2; v <= 10; v++) list.Add(new Card((Rank)v, s));
            list.Add(new Card(Rank.Jack,  s));
            list.Add(new Card(Rank.Queen, s));
            list.Add(new Card(Rank.King,  s));
            list.Add(new Card(Rank.Ace,   s));
        }
        list.Add(new Card(Rank.Joker, "None"));
        return list;
    }
    void DestroyChildrenOf(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    void Update()
    {
        if (Application.isPlaying && spawnOnToggle && !_prevSpawnOnToggle)
        {
            if (useDelayForTest)
                StartCoroutine(SpawnCountWithDelay(testSpawnCount, testDelayBetween));
            else
                SpawnCount(testSpawnCount);

            if (autoResetToggle) spawnOnToggle = false;
        }
        _prevSpawnOnToggle = spawnOnToggle;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var p = spawnPoints[i];
            if (!p) continue;
            Gizmos.DrawWireSphere(p.position, 0.25f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(p.position + Vector3.up * 0.35f, $"P{i+1}");
#endif
        }
    }
#endif
}
