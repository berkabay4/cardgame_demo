using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mystery/ActMysteryDatabase")]
public class ActMysteryDatabase : ScriptableObject
{
    public Act act = Act.Act1;

    [System.Serializable]
    public class Entry
    {
        public MysteryData data;
        [Min(0f)] public float weight = 1f; // Bu act için ağırlık
    }

    [Tooltip("Bu act için çıkabilecek MysteryData+Weight eşleşmeleri")]
    public List<Entry> mysteries = new();

    /// <summary>ID ile bul (id boş olanlar hariç)</summary>
    public MysteryData GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || mysteries == null) return null;
        return mysteries
            .Where(e => e != null && e.data != null && !string.IsNullOrEmpty(e.data.id))
            .Select(e => e.data)
            .FirstOrDefault(d => d.id == id);
    }

    /// <summary>Ağırlıklı rastgele seçim (Entry.weight kullanılır) - System.Random</summary>
    public bool TryGetRandomMystery(System.Random rng, out MysteryData picked)
    {
        picked = null;
        if (mysteries == null || mysteries.Count == 0) return false;

        var pool = mysteries.Where(e => e != null && e.data != null && e.weight > 0f).ToList();
        if (pool.Count == 0)
        {
            var fallback = mysteries.Where(e => e != null && e.data != null).ToList();
            if (fallback.Count == 0) return false;
            picked = fallback[rng.Next(0, fallback.Count)].data;
            return picked != null;
        }

        float total = pool.Sum(e => e.weight);
        float pick = (float)(rng.NextDouble() * total);
        float acc = 0f;

        foreach (var e in pool)
        {
            acc += e.weight;
            if (pick <= acc)
            {
                picked = e.data;
                return true;
            }
        }

        picked = pool[^1].data;
        return picked != null;
    }

    /// <summary>Ağırlıklı rastgele seçim - UnityEngine.Random</summary>
    public bool TryGetRandomMystery(out MysteryData picked)
    {
        picked = null;
        if (mysteries == null || mysteries.Count == 0) return false;

        var pool = mysteries.Where(e => e != null && e.data != null && e.weight > 0f).ToList();
        if (pool.Count == 0)
        {
            var fallback = mysteries.Where(e => e != null && e.data != null).ToList();
            if (fallback.Count == 0) return false;
            picked = fallback[Random.Range(0, fallback.Count)].data;
            return picked != null;
        }

        float total = pool.Sum(e => e.weight);
        float pick = Random.Range(0f, total);
        float acc = 0f;

        foreach (var e in pool)
        {
            acc += e.weight;
            if (pick <= acc)
            {
                picked = e.data;
                return true;
            }
        }

        picked = pool[^1].data;
        return picked != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor tarafında negatifleri sıfıra çek
        if (mysteries == null) return;
        foreach (var e in mysteries)
        {
            if (e != null && e.weight < 0f) e.weight = 0f;
        }
    }
#endif
}
