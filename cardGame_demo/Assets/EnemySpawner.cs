using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(3); // 0,1,2
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform parentForSpawned; // opsiyonel: sahneyi düzenli tutmak için

    [Header("Options")]
    [Tooltip("Spawn öncesi noktadaki önceki düşmanı temizle? (Basit kullanım için)")]
    [SerializeField] private bool destroyExistingOnPoint = true;

    [Header("Test (Inspector)")]
    [SerializeField, Min(0)] private int testSpawnCount = 1;
    [SerializeField] private bool spawnOnToggle = false;        // false -> true olduğunda spawn
    [SerializeField] private bool autoResetToggle = true;       // spawn sonrası tekrar false yap
    [SerializeField] private bool useDelayForTest = false;      // test spawn'ında gecikmeli sıralı spawn
    [SerializeField, Min(0f)] private float testDelayBetween = 0.2f;

    private bool _prevSpawnOnToggle;

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
            spawned.Add(go);
        }

        // Fazla noktalardaki eski düşmanları temizlemek istersen:
        if (destroyExistingOnPoint)
        {
            for (int i = n; i < spawnPoints.Count; i++)
                DestroyChildrenOf(spawnPoints[i]);
        }

        return spawned;
    }

    // Delay ile sırayla spawn (isteğe bağlı)
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

            Instantiate(enemyPrefab, p.position, p.rotation, parentForSpawned ? parentForSpawned : null);
            if (delayBetween > 0f) yield return new WaitForSeconds(delayBetween);
        }

        // Artan noktaları boşaltmak istersen
        if (destroyExistingOnPoint)
        {
            for (int i = n; i < spawnPoints.Count; i++)
                DestroyChildrenOf(spawnPoints[i]);
        }
    }

    private void DestroyChildrenOf(Transform t)
    {
        if (!t) return;
        // Bu noktaya daha önce parent ettiysen temizler
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private void Update()
    {
        // Inspector’da false -> true kenarını yakala (sadece Play Mode’da tetikleriz)
        if (Application.isPlaying && spawnOnToggle && !_prevSpawnOnToggle)
        {
            // Spawn tetikle
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
