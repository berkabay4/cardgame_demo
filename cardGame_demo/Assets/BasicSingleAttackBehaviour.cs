using System.Collections;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CardGame/Enemy/Elite Enemy/MiniBoss Behaviours/Basic Single Attack")]
public class BasicSingleAttackBehaviour : MiniBossAttackBehaviour
{
    [Header("Pattern")]
    [Tooltip("Kaçıncı enemy saldırı turunda çoklu vuruş yapılacağını belirler. Örn: 2 → her 2. turda.")]
    public int multiHitEveryNthAttack = 2;

    [Tooltip("Multi hit turunda kaç kez vurulacağını belirler. Örn: 3 → 3x hit.")]
    public int multiHitCount = 3;

    [Tooltip("Her vuruş arası küçük gecikme (sn).")]
    public float delayBetweenHits = 0.15f;

    public override void ExecuteAttack(CombatContext ctx, MiniBossRuntime boss, int baseAttackValue, int attackRoundIndex)
    {
        if (ctx == null || boss == null || boss.Definition == null)
            return;

        // CombatDirector hem ICoroutineHost hem IAnimationBridge
        var director = CombatDirector.Instance;
        if (director == null)
        {
            // Fallback: animasyon yok, direkt hasar uygula
            int hitsFallback = ComputeHitCount(attackRoundIndex);
            for (int i = 0; i < hitsFallback; i++)
                DealDamageToPlayer(ctx, baseAttackValue);

            Debug.LogWarning("[MiniBoss] CombatDirector yok, BasicSingleAttackBehaviour fallback ile direkt hasar vurdu.");
            return;
        }

        // Animasyonlu saldırı coroutine’ini başlat
        director.Run(AttackRoutine(ctx, boss, baseAttackValue, attackRoundIndex, director));
    }

    IEnumerator AttackRoutine(
        CombatContext ctx,
        MiniBossRuntime boss,
        int baseAttackValue,
        int attackRoundIndex,
        CombatDirector director)
    {
        var enemySC  = boss.GetComponent<SimpleCombatant>();
        var playerSC = director.Player;

        if (enemySC == null || playerSC == null)
            yield break;

        int hits = ComputeHitCount(attackRoundIndex);
        int damagePerHit = Mathf.Max(0, baseAttackValue);

        Debug.Log($"[MiniBoss] {boss.Definition.displayName} enemyAttackRound={attackRoundIndex}, hits={hits}, dmgPerHit={damagePerHit}");

        for (int i = 0; i < hits; i++)
        {
            // Her hit için ayrı animasyon:
            yield return director.PlayAttackAnimation(
                enemySC,
                playerSC,
                damagePerHit,
                () =>
                {
                    // Impact anında gerçek hasarı uygula
                    DealDamageToPlayer(ctx, damagePerHit);
                });

            if (delayBetweenHits > 0f)
                yield return new WaitForSeconds(delayBetweenHits);
        }
    }

    /// <summary>
    /// Bu pattern’de:
    ///  - Normal turlar → 1x hit
    ///  - Her N. tur (örn: N=2) → multiHitCount kez vur.
    /// </summary>
    int ComputeHitCount(int attackRoundIndex)
    {
        if (multiHitEveryNthAttack > 0 &&
            attackRoundIndex > 0 &&
            attackRoundIndex % multiHitEveryNthAttack == 0)
        {
            return Mathf.Max(1, multiHitCount); // örn: 3
        }

        return 1; // normal tur
    }

    void DealDamageToPlayer(CombatContext ctx, int amount)
    {
        if (ctx == null || amount <= 0) return;

        var playerUnit = ctx.Player;
        if (playerUnit == null) return;

        var hm = playerUnit.GetComponent<HealthManager>();
        if (hm == null)
        {
            Debug.LogWarning("[MiniBoss] Player HealthManager bulunamadı, hasar uygulanamadı.");
            return;
        }

        hm.TakeDamage(amount); // kendi Damage/TakeDamage metodunun adı neyse onu kullan
        Debug.Log($"[MiniBoss] Player {amount} damage aldı.");
    }
}
