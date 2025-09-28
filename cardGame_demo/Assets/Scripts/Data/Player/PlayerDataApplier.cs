using UnityEngine;

public static class PlayerDataApplier
{
    /// <summary>
    /// PlayerData değerlerini hedef SimpleCombatant'a uygular.
    /// Öncelik: PlayerStats varsa ondan init → yoksa Combatant alanlarına yazar.
    /// </summary>
    public static void ApplyTo(this PlayerData data, SimpleCombatant target)
    {
        if (!data || !target) return;

        // İsim
        if (!string.IsNullOrWhiteSpace(data.playerName))
            target.name = data.playerName;

        // Sprite
        var sr = target.GetComponentInChildren<SpriteRenderer>(true) ?? target.GetComponent<SpriteRenderer>();
        if (sr && data.playerSprite)
            sr.sprite = data.playerSprite;

        // Öncelik: PlayerStats varsa oradan init et
        var stats = target.GetComponentInChildren<PlayerStats>(true) ?? target.GetComponent<PlayerStats>();
        if (stats)
        {
            stats.InitFrom(data);                 // MaxHealth, CurrentHealth=Max, MaxRange(21) vb.
            // SimpleCombatant ile senkron (HP’yi doldur)
            target.CurrentHP = stats.MaxHealth;

            // SimpleCombatant'ta public field 'maxHP' varsa hizala (senin paylaştığın sınıfta public)
            target.maxHP = stats.MaxHealth;
        }
        else
        {
            // Fallback: PlayerStats yoksa direkt Combatant alanlarına yaz
            int hp = Mathf.Max(1, data.maxHealth);
            target.maxHP   = hp;                  // public field (paylaştığın koddaki gibi)
            target.CurrentHP = hp;
        }

        // Not: Kurallar (maxRange) oyun genel kural yöneticisinden okunuyorsa
        // burada bulup set edebilirsin (örn. BlackjackRules.SetMaxRange(data.maxRange)).
        // Bu satırlar projendeki kural akışına göre eklenebilir.
    }
}
