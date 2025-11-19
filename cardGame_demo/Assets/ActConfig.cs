using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Act Config", fileName = "ActConfig")]
public class ActConfig : ScriptableObject
{
    [Header("Identity")]
    public Act act;

    // === ENEMIES ===
    [System.Serializable]
    public class ActMinorEnemyEntry
    {
        [Tooltip("Bu act’te spawn olabilecek normal düşman (EnemyDatabase içindeki EnemyData’lardan biri).")]
        public EnemyData enemy;

        [Tooltip("Weighted random için ağırlık. 1 = normal, 5 = çok sık, 0 = hiç çıkmaz.")]
        public int weight = 1;
    }

    [System.Serializable]
    public class ActEliteEnemyEntry
    {
        [Tooltip("Bu act’te spawn olabilecek miniboss tanımı.")]
        public MiniBossDefinition miniBoss;

        [Tooltip("Weighted random için ağırlık. 1 = normal, 5 = çok sık, 0 = hiç çıkmaz.")]
        public int weight = 1;
    }

    [Header("Combat / Enemies")]
    [Tooltip("Normal (minor) encounter’larda kullanılacak düşman havuzu.\n" +
             "Buradaki EnemyData referanslarını EnemyDatabase’den seçebilirsin.")]
    public ActMinorEnemyEntry[] minorEnemies;

    [Tooltip("Elite encounter / mini-boss için kullanılacak miniboss havuzu.")]
    public ActEliteEnemyEntry[] eliteEnemies;

    // === RELICS ===
    [System.Serializable]
    public class ActRelicEntry
    {
        [Tooltip("Bu act’te droplanabilecek relic.")]
        public RelicDefinition relic;

        [Tooltip("Weighted random için ağırlık. 1 = normal, 5 = çok sık, 0 = hiç çıkmaz.")]
        public int weight = 1;
    }

    [Header("Rewards")]
    [Tooltip("Bu act’te kullanılabilecek relic havuzu ve ağırlıkları.")]
    public ActRelicEntry[] relicPool;

    // === MYSTERY ===
    [System.Serializable]
    public class ActMysteryEntry
    {
        [Tooltip("Bu act’te çıkabilecek mystery data (id + sceneName vs. burada).")]
        public MysteryData data;

        [Tooltip("Weighted random seçim için ağırlık (1 = normal).")]
        public int weight = 1;
    }

    [Header("Mysteries")]
    [Tooltip("Bu act’te kullanılabilecek tüm mystery’ler ve ağırlıkları.")]
    public ActMysteryEntry[] mysteries;

    // --- MYSTERY HELPERS ---

    public MysteryData GetMysteryById(string mysteryId)
    {
        if (string.IsNullOrEmpty(mysteryId) || mysteries == null) return null;

        foreach (var entry in mysteries)
        {
            if (entry?.data == null) continue;
            if (entry.data.id == mysteryId)
                return entry.data;
        }

        return null;
    }

    public bool TryGetRandomMystery(System.Random rng, out MysteryData result)
    {
        result = null;

        if (mysteries == null || mysteries.Length == 0)
            return false;

        int totalWeight = 0;
        foreach (var entry in mysteries)
        {
            if (entry == null || entry.data == null) continue;
            if (entry.weight <= 0) continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in mysteries)
        {
            if (entry == null || entry.data == null) continue;
            if (entry.weight <= 0) continue;

            cumulative += entry.weight;
            if (roll < cumulative)
            {
                result = entry.data;
                return true;
            }
        }

        return false;
    }

    // --- ENEMY HELPERS (weighted) ---

    public EnemyData GetRandomMinorEnemy(System.Random rng)
    {
        if (minorEnemies == null || minorEnemies.Length == 0) return null;

        int totalWeight = 0;
        foreach (var entry in minorEnemies)
        {
            if (entry == null || entry.enemy == null) continue;
            if (entry.weight <= 0) continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0) return null;

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in minorEnemies)
        {
            if (entry == null || entry.enemy == null) continue;
            if (entry.weight <= 0) continue;

            cumulative += entry.weight;
            if (roll < cumulative)
            {
                return entry.enemy;
            }
        }

        return null;
    }

    public MiniBossDefinition GetRandomEliteEnemy(System.Random rng)
    {
        if (eliteEnemies == null || eliteEnemies.Length == 0) return null;

        int totalWeight = 0;
        foreach (var entry in eliteEnemies)
        {
            if (entry == null || entry.miniBoss == null) continue;
            if (entry.weight <= 0) continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0) return null;

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in eliteEnemies)
        {
            if (entry == null || entry.miniBoss == null) continue;
            if (entry.weight <= 0) continue;

            cumulative += entry.weight;
            if (roll < cumulative)
            {
                return entry.miniBoss;
            }
        }

        return null;
    }

    // --- RELIC HELPERS (weighted) ---

    public bool TryGetRandomRelic(System.Random rng, out RelicDefinition relic)
    {
        relic = null;

        if (relicPool == null || relicPool.Length == 0)
            return false;

        int totalWeight = 0;
        foreach (var entry in relicPool)
        {
            if (entry == null || entry.relic == null) continue;
            if (entry.weight <= 0) continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in relicPool)
        {
            if (entry == null || entry.relic == null) continue;
            if (entry.weight <= 0) continue;

            cumulative += entry.weight;
            if (roll < cumulative)
            {
                relic = entry.relic;
                return true;
            }
        }

        return false;
    }
}
