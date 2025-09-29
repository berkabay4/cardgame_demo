using UnityEngine;
using System; // string.IsNullOrWhiteSpace

public static class EnemyDataApplier
{
    public static void ApplyTo(this EnemyData data, SimpleCombatant target)
    {
        if (!data || !target) return;

        // İsim
        if (!string.IsNullOrWhiteSpace(data.enemyName))
            target.name = data.enemyName;

        // === Health (HealthManager üzerinden) ===
        var hm = target.GetComponentInChildren<HealthManager>(true) ?? target.GetComponent<HealthManager>();
        if (!hm) hm = target.gameObject.AddComponent<HealthManager>();

        int newMax = Mathf.Max(1, data.maxHealth);
        hm.SetMaxHP(newMax, keepRatio: false); // oranı koruma, direkt yeni max'a geç
        hm.RefillToMax();                      // CurrentHP = MaxHP
        hm.SetBlock(0);                        // başlangıçta block temiz (opsiyonel)

        // Sprite (varsa)
        var sr = target.GetComponentInChildren<SpriteRenderer>(true) ?? target.GetComponent<SpriteRenderer>();
        if (sr && data.enemySprite) sr.sprite = data.enemySprite;

        // (Gerekirse burada başka EnemyData alanlarını da uygula:
        //  - başlangıç buff/debuff,
        //  - AI parametreleri,
        //  - saldırı aralıkları vb.)
    }
}
