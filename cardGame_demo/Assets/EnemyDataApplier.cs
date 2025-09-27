using UnityEngine;

public static class EnemyDataApplier
{
    public static void ApplyTo(this EnemyData data, SimpleCombatant target)
    {
        if (!data || !target) return;

        // İsim
        target.name = string.IsNullOrWhiteSpace(data.enemyName) ? target.name : data.enemyName;

        // Can
        target.maxHP = Mathf.Max(1, data.maxHealth);
        target.CurrentHP = target.MaxHP;

        // Sprite (varsa)
        var sr = target.GetComponentInChildren<SpriteRenderer>(true) ?? target.GetComponent<SpriteRenderer>();
        if (sr && data.enemySprite) sr.sprite = data.enemySprite;

        // (İstersen burada target üstünde başka alanlar/AI parametreleri set edebilirsin)
        // Örn: target.AttackHintMin = data.attackRange.min; vs.
    }
}
