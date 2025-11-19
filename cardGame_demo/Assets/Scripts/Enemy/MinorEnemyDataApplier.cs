using UnityEngine;
using System; // string.IsNullOrWhiteSpace

public static class MinorEnemyDataApplier
{
    /// <summary>
    /// EnemyData'daki bilgileri SimpleCombatant üstüne uygular:
    /// - İsim
    /// - HealthManager (MaxHP + full heal + block=0)
    /// - Sprite
    /// </summary>
    public static void ApplyTo(this EnemyData data, SimpleCombatant target)
    {
        if (!data)
        {
            Debug.LogWarning("[MinorEnemyDataApplier] data null, apply iptal.");
            return;
        }

        if (!target)
        {
            Debug.LogWarning($"[MinorEnemyDataApplier] target null. EnemyData: {data.name}");
            return;
        }

        // === İsim ===
        if (!string.IsNullOrWhiteSpace(data.enemyName))
        {
            // Hem GameObject ismi hem combatant ismi güncellensin
            target.name = data.enemyName;
            target.gameObject.name = data.enemyName;
        }

        // === Health (HealthManager üzerinden) ===
        var hm = target.GetComponentInChildren<HealthManager>(true) 
                 ?? target.GetComponent<HealthManager>();

        if (!hm)
        {
            // Eğer prefab'ta hiç yoksa ekleyelim (tercihine göre bu kısmı kaldırabilirsin)
            hm = target.gameObject.AddComponent<HealthManager>();
            Debug.Log($"[MinorEnemyDataApplier] HealthManager eklendi → {target.gameObject.name}");
        }

        if (hm)
        {
            int newMax = Mathf.Max(1, data.maxHealth);
            hm.SetMaxHP(newMax, keepRatio: false); // oranı koruma, direkt yeni max'a geç
            hm.RefillToMax();                      // CurrentHP = MaxHP
            hm.SetBlock(0);                        // başlangıçta block temiz (opsiyonel)
        }

        // === Sprite (varsa) ===
        var sr = target.GetComponentInChildren<SpriteRenderer>(true) 
                 ?? target.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            if (data.sprite != null)
            {
                sr.sprite = data.sprite;
                // Debug amaçlı log; istersen yorum satırı yapabilirsin
                // Debug.Log($"[MinorEnemyDataApplier] Sprite set: {target.gameObject.name} -> {data.sprite.name} (renderer: {sr.gameObject.name})");
            }
            else
            {
                Debug.LogWarning($"[MinorEnemyDataApplier] EnemyData.sprite atanmadı. EnemyData: {data.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[MinorEnemyDataApplier] SpriteRenderer bulunamadı. Target: {target.gameObject.name}");
        }

        // İleride:
        //  - AI parametreleri
        //  - saldırı aralıkları
        //  - başlangıç buff/debuff
        // vs. eklenecekse burada uygulayabilirsin.
    }
}
