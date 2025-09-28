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
    private readonly List<(SimpleCombatant sc, IDeckService deck)> _pendingCtxRegs = new();
    private bool _prevSpawnOnToggle;

    // ---- Events ----
    [Header("Events")]
    public UnityEvent onEnemiesSpawned;               // tüm spawnlar bitti
    public UnityEvent<GameObject> onEnemySpawned;     // her yeni düşman için
    public static event System.Action EnemiesSpawned; // global (isteğe bağlı)

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
            var go   = SpawnFromData(p, data);
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
        GameDirector.ContextReady += FlushPendingContextRegs;
    }
    void OnDisable()
    {
        GameDirector.ContextReady -= FlushPendingContextRegs;
    }
    void FlushPendingContextRegs()
    {
        var ctx = GameDirector.Instance ? GameDirector.Instance.Ctx : null;
        if (ctx == null || _pendingCtxRegs.Count == 0) return;

        foreach (var (sc, deck) in _pendingCtxRegs)
        {
            if (sc && deck != null)
            {
                ctx.RegisterEnemy(sc, deck); // son kayıt “current enemy” olur (normal)
                Debug.Log($"[EnemySpawner] Pending enemy registered to Context: {sc.name}");
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

        var owner = go.GetComponentInChildren<DeckOwner>(true) ?? go.AddComponent<DeckOwner>();
        int seed = (go.GetInstanceID() ^ Random.Range(0, int.MaxValue));
        owner.CreateNewDeck(seed);

        var handle = go.GetComponentInChildren<DeckHandle>(true) ?? go.AddComponent<DeckHandle>();
        handle.Bind(owner.Deck);

        var sc  = go.GetComponentInChildren<SimpleCombatant>(true);
        var ctx = GameDirector.Instance ? GameDirector.Instance.Ctx : null;

        if (ctx != null && sc != null)
        {
            ctx.RegisterEnemy(sc, owner.Deck);
            Debug.Log($"[EnemySpawner] Registered enemy to CombatContext: {sc.name}");
        }
        else
        {
            if (sc != null)
            {
                _pendingCtxRegs.Add((sc, owner.Deck));   // <-- beklet
                Debug.LogWarning("[EnemySpawner] Context not ready — queued pending registration.");
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] SimpleCombatant missing — cannot queue registration.");
            }
        }
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
