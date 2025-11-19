using UnityEngine;

/// <summary>
/// MiniBossDefinition verisini sahnedeki miniboss GameObject'ine uygular.
/// Buradan:
///  - MiniBossRuntime.Init(def)
///  - SpriteRenderer'a portrait atanması
/// gibi işlemler tek noktadan yönetilir.
/// </summary>
public static class EliteEnemyDataApplier
{
    /// <summary>
    /// MiniBossDefinition'daki verileri verilen GameObject'e uygular.
    /// </summary>
    public static void Apply(MiniBossDefinition def, GameObject go)
    {
        if (def == null || !go)
        {
            Debug.LogWarning("[EliteEnemyDataApplier] Apply çağrıldı ama def veya go null.");
            return;
        }
        // Set maximum HP
        SimpleCombatant simpleCombatant = go.GetComponent<SimpleCombatant>();
        if (simpleCombatant != null)
        {
             var hm = simpleCombatant.GetComponentInChildren<HealthManager>(true) ?? simpleCombatant.GetComponent<HealthManager>();
            if (!hm) hm = simpleCombatant.gameObject.AddComponent<HealthManager>();

            int newMax = Mathf.Max(1, def.maxHealth);
            hm.SetMaxHP(newMax, keepRatio: false); // oranı koruma, direkt yeni max'a geç
            hm.RefillToMax();                      // CurrentHP = MaxHP
            hm.SetBlock(0);                        // başlangıçta block temiz (opsiyonel)

        }
        // 1) MiniBossRuntime init
        var mini = go.GetComponent<MiniBossRuntime>();
        if (mini != null)
        {
            mini.Init(def);
        }
        else
        {
            Debug.LogWarning("[EliteEnemyDataApplier] MiniBossRuntime component bulunamadı. Init çalışmadı.");
        }

        // 2) Sprite'ı portrait'ten set et (varsa)
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && def.sprite != null)
        {
            sr.sprite = def.sprite;
        }
    }
}
