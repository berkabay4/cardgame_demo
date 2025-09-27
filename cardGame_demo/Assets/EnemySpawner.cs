using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(3);
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform parentForSpawned;

    [Header("Options")]
    [SerializeField] private bool destroyExistingOnPoint = true;

    [Header("Test (Inspector)")]
    [SerializeField, Min(0)] private int testSpawnCount = 1;
    [SerializeField] private bool spawnOnToggle = false;     // false -> true olduğunda spawn
    [SerializeField] private bool autoResetToggle = true;
    [SerializeField] private bool useDelayForTest = false;
    [SerializeField, Min(0f)] private float testDelayBetween = 0.2f;

    private bool _prevSpawnOnToggle;

    // ---- Events ----
    [Header("Events")]
    public UnityEvent onEnemiesSpawned;                  // tüm spawnlar bitti
    public UnityEvent<GameObject> onEnemySpawned;        // her yeni düşman için
    public static event System.Action EnemiesSpawned;    // global (isteğe bağlı)

    // Basit: anında spawn – ilk N noktayı doldur
    public List<GameObject> SpawnCount(int count)
    {
        var spawned = new List<GameObject>();

        if (!enemyPrefab || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Prefab ya da spawn point listesi eksik.");
            return spawned;
        }

        int n = Mathf.Clamp(count, 0, spawnPoints.Count);

        for (int i = 0; i < n; i++)
        {
            var p = spawnPoints[i];
            if (!p) continue;

            if (destroyExistingOnPoint)
                DestroyChildrenOf(p);

            var go = Instantiate(enemyPrefab, p.position, p.rotation, parentForSpawned ? parentForSpawned : null);

            // parent noktaya almak istersen:
            // if (!parentForSpawned) go.transform.SetParent(p);
            // META ekle / doldur
            var meta = go.GetComponent<EnemySpawnMeta>();
            if (!meta) meta = go.AddComponent<EnemySpawnMeta>();
            meta.source = this;
            meta.spawnPoint = p;
            meta.spawnIndex = i;

            // mevcut kodun devamı...
            spawned.Add(go);
            onEnemySpawned?.Invoke(go);
            spawned.Add(go);

            // per-enemy event
            onEnemySpawned?.Invoke(go);
        }

        // Kullanılmayan noktalardakileri temizle
        if (destroyExistingOnPoint)
        {
            for (int i = n; i < spawnPoints.Count; i++)
                DestroyChildrenOf(spawnPoints[i]);
        }

        // batch bitti
        onEnemiesSpawned?.Invoke();
        EnemiesSpawned?.Invoke();

        return spawned;
    }

    // Delay ile sırayla spawn
    public IEnumerator SpawnCountWithDelay(int count, float delayBetween = 0.2f)
    {
        if (!enemyPrefab || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Prefab ya da spawn point listesi eksik.");
            yield break;
        }

        int n = Mathf.Clamp(count, 0, spawnPoints.Count);
        for (int i = 0; i < n; i++)
        {
            var p = spawnPoints[i];
            if (!p) continue;

            if (destroyExistingOnPoint)
                DestroyChildrenOf(p);

            var go = Instantiate(enemyPrefab, p.position, p.rotation, parentForSpawned ? parentForSpawned : null);
            var meta = go.GetComponent<EnemySpawnMeta>();
            if (!meta) meta = go.AddComponent<EnemySpawnMeta>();
            meta.source = this;
            meta.spawnPoint = p;
            meta.spawnIndex = i;
            onEnemySpawned?.Invoke(go);
            
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

    private void DestroyChildrenOf(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private void Update()
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
    private void OnDrawGizmosSelected()
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
