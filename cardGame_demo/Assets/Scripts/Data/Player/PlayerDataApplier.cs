using UnityEngine;
using System; // string.IsNullOrWhiteSpace

public static class PlayerDataApplier
{
    /// <summary>
    /// PlayerData değerlerini hedef SimpleCombatant'a uygular.
    /// Öncelik: PlayerStats varsa ondan init → ardından HealthManager'a uygula.
    /// </summary>
    public static void ApplyTo(this PlayerData data, SimpleCombatant target)
    {
        if (!data || !target) return;

        // İsim
        if (!string.IsNullOrWhiteSpace(data.playerName))
            target.name = data.playerName;

        // Sprite
        var sr = target.GetComponentInChildren<SpriteRenderer>(true) ?? target.GetComponent<SpriteRenderer>();
        if (sr && data.playerSprite) sr.sprite = data.playerSprite;

        // HealthManager hazırla (yoksa ekle)
        var hm = target.GetComponentInChildren<HealthManager>(true) ?? target.GetComponent<HealthManager>();
        if (!hm) hm = target.gameObject.AddComponent<HealthManager>();

        // PlayerStats varsa ondan init et (senin Stats.InitFrom(data) metodunu kullanarak)
        var stats = target.GetComponentInChildren<PlayerStats>(true) ?? target.GetComponent<PlayerStats>();
        if (stats)
        {
            // PlayerStats kendi iç alanlarını doldursun (MaxHealth vb.)
            stats.InitFrom(data);

            // HealthManager'a yaz (refill opsiyonu ile)
            int newMax = Mathf.Max(1, stats.MaxHealth);
            hm.SetMaxHP(newMax, keepRatio: false); // stats'tan gelen değere tam geç
            hm.RefillToMax();                      // CurrentHP = MaxHP
            hm.SetBlock(0);                        // başlangıçta block temiz (opsiyonel)
        }
        else
        {
            // Fallback: PlayerStats yoksa direkt PlayerData'dan HealthManager'a yaz
            int newMax = Mathf.Max(1, data.maxHealth);
            hm.SetMaxHP(newMax, keepRatio: false);
            hm.RefillToMax();
            hm.SetBlock(0); // opsiyonel
        }

        // NOT: Attack/Defense threshold veya diğer savaş statları PlayerStats/Rule sisteminden geliyorsa
        // onları GameDirector/RelicStatsSync tarafında senkronize etmeye devam edelim.
    }
}
