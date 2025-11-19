using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CombatScene içindeki düşman spawn sistemi.
/// Artık düşmanlar:
///  - Minor encounter → ActConfig.minorEnemies (EnemyData, weighted)
///  - Elite encounter → ActConfig.eliteEnemies (MiniBossDefinition, weighted)
/// üzerinden seçiliyor.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum EncounterEnemyType
    {
        Minor,
        EliteMiniBoss
    }

    [Header("Setup")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(3);

    [Tooltip("Generic prefab (MINOR düşmanlar için). EnemyData.enemyPrefab yoksa bu kullanılır.")]
    [SerializeField] private GameObject minorEnemyPrefab;

    [Tooltip("Generic prefab (ELITE / miniboss için). MiniBossDefinition.prefab yoksa bu kullanılır.")]
    [SerializeField] private GameObject eliteEnemyPrefab;

    [SerializeField] private Transform parentForSpawned;

    [Header("Encounter Type")]
    [Tooltip("Bu combat’ta hangi tür düşman spawnlanacak. Bunu Combat'a geçerken ayarlamanı öneririm.")]
    [SerializeField] private EncounterEnemyType encounterType = EncounterEnemyType.Minor;

    [Header("Options")]
    [SerializeField] private bool destroyExistingOnPoint = true;
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
    public UnityEvent onEnemiesSpawned;               // tüm spawnlar bittiğinde
    public UnityEvent<GameObject> onEnemySpawned;     // her yeni düşman için
    public static event System.Action EnemiesSpawned; // global (isteğe bağlı)

    private struct PendingEnemy
    {
        public SimpleCombatant sc;
        public IDeckService deck;
        public int atkMax;
        public int defMax;
    }

    private readonly List<PendingEnemy> _pendingCtxRegs = new();

    // ==================== Public API ====================

    public void SetEncounterType(EncounterEnemyType type)
    {
        encounterType = type;
    }

    public List<GameObject> SpawnCount(int count)
    {
        var spawned = new List<GameObject>();

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No spawn points.");
            return spawned;
        }

        var gsd = GameSessionDirector.Instance;
        var actConfig = gsd ? gsd.CurrentActConfig : null;
        var run = gsd ? gsd.Run : null;
        if (actConfig == null)
        {
            Debug.LogWarning("[EnemySpawner] No ActConfig found via GameSessionDirector.");
            return spawned;
        }

        int seed = run != null && run.pendingEncounter != null
            ? run.pendingEncounter.seed
            : Random.Range(int.MinValue, int.MaxValue);
        var rng = new System.Random(seed);

        var type = ResolveEncounterType(run);

        if (type == EncounterEnemyType.Minor)
        {
            // === MINOR ENCOUNTER ===
            int n = Mathf.Clamp(count, 0, spawnPoints.Count);

            for (int i = 0; i < n; i++)
            {
                var p = spawnPoints[i];
                if (!p) continue;

                if (destroyExistingOnPoint) DestroyChildrenOf(p);

                var data = actConfig.GetRandomMinorEnemy(rng);
                if (data == null)
                {
                    Debug.LogWarning("[EnemySpawner] ActConfig’ten minor enemy seçilemedi.");
                    continue;
                }

                var go = SpawnMinorFromData(p, data);
                if (!go) continue;

                SetupEnemyRuntime(go, data);   // DECK + CONTEXT
                spawned.Add(go);
                onEnemySpawned?.Invoke(go);
            }

            // kullanılmayan noktaları boşalt
            if (destroyExistingOnPoint)
            {
                for (int i = n; i < spawnPoints.Count; i++)
                    DestroyChildrenOf(spawnPoints[i]);
            }
        }
        else
        {
            // === ELITE / MINI BOSS ENCOUNTER ===
            var p = spawnPoints[0];
            if (!p)
            {
                Debug.LogWarning("[EnemySpawner] No spawn point for miniboss.");
                return spawned;
            }

            if (destroyExistingOnPoint) DestroyChildrenOf(p);

            var miniBossDef = actConfig.GetRandomEliteEnemy(rng);
            if (miniBossDef == null)
            {
                Debug.LogWarning("[EnemySpawner] ActConfig’ten miniboss seçilemedi.");
                return spawned;
            }

            var go = SpawnMiniBossFromDefinition(p, miniBossDef);
            if (go != null)
            {
                SetupEnemyRuntime(go, null); // EnemyData yok → threshold fallback kullan
                spawned.Add(go);
                onEnemySpawned?.Invoke(go);
            }

            if (destroyExistingOnPoint && spawnPoints.Count > 1)
            {
                for (int i = 1; i < spawnPoints.Count; i++)
                    DestroyChildrenOf(spawnPoints[i]);
            }
        }

        onEnemiesSpawned?.Invoke();
        EnemiesSpawned?.Invoke();
        return spawned;
    }

    public IEnumerator SpawnCountWithDelay(int count, float delayBetween = 0.2f)
    {
        if (spawnPoints.Count == 0) yield break;

        var gsd = GameSessionDirector.Instance;
        var actConfig = gsd ? gsd.CurrentActConfig : null;
        var run = gsd ? gsd.Run : null;
        if (actConfig == null)
        {
            Debug.LogWarning("[EnemySpawner] No ActConfig found via GameSessionDirector.");
            yield break;
        }

        int seed = run != null && run.pendingEncounter != null
            ? run.pendingEncounter.seed
            : Random.Range(int.MinValue, int.MaxValue);
        var rng = new System.Random(seed);

        var type = ResolveEncounterType(run);

        if (type == EncounterEnemyType.Minor)
        {
            int n = Mathf.Clamp(count, 0, spawnPoints.Count);

            for (int i = 0; i < n; i++)
            {
                var p = spawnPoints[i];
                if (!p) continue;

                if (destroyExistingOnPoint) DestroyChildrenOf(p);

                var data = actConfig.GetRandomMinorEnemy(rng);
                if (data != null)
                {
                    var go = SpawnMinorFromData(p, data);
                    if (go)
                    {
                        SetupEnemyRuntime(go, data);
                        onEnemySpawned?.Invoke(go);
                    }
                }

                if (delayBetween > 0f) yield return new WaitForSeconds(delayBetween);
            }

            if (destroyExistingOnPoint)
            {
                for (int i = n; i < spawnPoints.Count; i++)
                    DestroyChildrenOf(spawnPoints[i]);
            }
        }
        else
        {
            // Elite / miniboss: tek spawn
            var p = spawnPoints[0];
            if (!p)
            {
                Debug.LogWarning("[EnemySpawner] No spawn point for miniboss.");
                yield break;
            }

            if (destroyExistingOnPoint) DestroyChildrenOf(p);

            var miniBossDef = actConfig.GetRandomEliteEnemy(rng);
            if (miniBossDef != null)
            {
                var go = SpawnMiniBossFromDefinition(p, miniBossDef);
                if (go)
                {
                    SetupEnemyRuntime(go, null);
                    onEnemySpawned?.Invoke(go);
                }
            }

            if (delayBetween > 0f) yield return new WaitForSeconds(delayBetween);

            if (destroyExistingOnPoint && spawnPoints.Count > 1)
            {
                for (int i = 1; i < spawnPoints.Count; i++)
                    DestroyChildrenOf(spawnPoints[i]);
            }
        }

        onEnemiesSpawned?.Invoke();
        EnemiesSpawned?.Invoke();
    }

    // ==================== Lifecycle / Context ====================

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
            }
        }
        _pendingCtxRegs.Clear();
    }

    // ==================== Internals ====================

    private EncounterEnemyType ResolveEncounterType(RunContext run)
    {
        // RunContext ve pendingEncounter varsa nodeType'a göre karar ver
        if (run != null && run.pendingEncounter != null)
        {
            // ⚠️ Buradaki enum değerlerini senin Map.NodeType'ına göre güncellemen gerekiyor
            switch (run.pendingEncounter.nodeType)
            {
                case Map.NodeType.MinorEnemy:
                    return EncounterEnemyType.Minor;

                case Map.NodeType.EliteEnemy:
                    return EncounterEnemyType.EliteMiniBoss;

                    // istersen boss, event vs. için extra case'ler eklersin
            }
        }
        else
        Debug.LogError("[EnemySpawner] RunContext veya pendingEncounter null iken ResolveEncounterType çağrıldı.");

            // Fallback:
            // - Test ederken Inspector'dan encounterType seçebil diye
            // - Ya da nodeType tanınmazsa
            return encounterType;
    }

    GameObject SpawnMinorFromData(Transform point, EnemyData data)
    {
        // Önce data.enemyPrefab, yoksa generic minorEnemyPrefab
        GameObject prefabToUse = (data && data.enemyPrefab) ? data.enemyPrefab : minorEnemyPrefab;
        if (!prefabToUse)
        {
            Debug.LogWarning("[EnemySpawner] No prefab to spawn for MINOR enemy (hem EnemyData.enemyPrefab hem minorEnemyPrefab boş).");
            return null;
        }

        var go = Instantiate(prefabToUse, point.position, point.rotation,
                             parentForSpawned ? parentForSpawned : null);

        // spawn meta
        var meta = go.GetComponent<EnemySpawnMeta>() ?? go.AddComponent<EnemySpawnMeta>();
        meta.source     = this;
        meta.spawnPoint = point;
        meta.spawnIndex = spawnPoints.IndexOf(point);

        // data uygula (SimpleCombatant varsayımı)
        var sc   = go.GetComponent<SimpleCombatant>();
        var prov = go.GetComponent<EnemyTargetRangeProvider>() ?? go.AddComponent<EnemyTargetRangeProvider>();
        prov.enemyData = data; // Attack/Defense stand aralıkları buradan okunacak
        if (data && sc)
        {
            MinorEnemyDataApplier.ApplyTo(data, sc);
        }

        // Sprite'ı data.sprite'tan set et (eğer varsa)
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && data != null && data.sprite != null)
        {
            sr.sprite = data.sprite;
        }

        return go;
    }

    GameObject SpawnMiniBossFromDefinition(Transform point, MiniBossDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[EnemySpawner] MiniBossDefinition null.");
            return null;
        }

        // Önce def.prefab, yoksa generic eliteEnemyPrefab
        var prefabToUse = def.prefab ? def.prefab : eliteEnemyPrefab;
        if (!prefabToUse)
        {
            Debug.LogWarning($"[EnemySpawner] MiniBoss '{def.displayName}' için prefab bulunamadı (hem def.prefab hem eliteEnemyPrefab boş).");
            return null;
        }

        var go = Instantiate(prefabToUse, point.position, point.rotation,
                             parentForSpawned ? parentForSpawned : null);

        // spawn meta
        var meta = go.GetComponent<EnemySpawnMeta>() ?? go.AddComponent<EnemySpawnMeta>();
        meta.source     = this;
        meta.spawnPoint = point;
        meta.spawnIndex = spawnPoints.IndexOf(point);

        // Tüm miniboss data uygulamasını merkezi helper’a devret
        EliteEnemyDataApplier.Apply(def, go);

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
            deck = combatantDeck.BuildDeck();
        }
        else
        {
            Debug.LogError("[EnemySpawner] No CombatantDeck found on enemy prefab. Please add one for proper deck setup.");
        }

        owner.SetDeck(deck as DeckService ?? new DeckService());
        handle.Bind(owner.Deck);

        // ---- 3) Context kaydı + threshold ----
        var sc  = go.GetComponentInChildren<SimpleCombatant>(true);
        var ctx = CombatDirector.Instance ? CombatDirector.Instance.Ctx : null;

        int atkMax = (data != null) ? Mathf.Max(5, data.maxAttackRange)  : ((ctx != null) ? ctx.Threshold : 21);
        int defMax = (data != null) ? Mathf.Max(5, data.maxdefenceRange) : ((ctx != null) ? ctx.Threshold : 21);

        if (registerToContext && ctx != null && sc != null)
        {
            ctx.RegisterEnemy(sc, owner.Deck);
            ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Attack,  atkMax);
            ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Defense, defMax);
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

    // ==================== Helpers ====================

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
