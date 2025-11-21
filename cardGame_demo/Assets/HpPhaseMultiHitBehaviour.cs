using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu(
    menuName = "CardGame/Enemy/Elite Enemy/MiniBoss Behaviours/HP Phase Multi Hit",
    fileName = "HpPhaseMultiHitBehaviour")]
public class HpPhaseMultiHitBehaviour : MiniBossAttackBehaviour
{
    [System.Serializable]
    public struct HpPhase
    {
        [Tooltip("Bu eşik VE ALTI için geçerli HP oranı (0-1). Örn: 0.7 = %70 ve altı.")]
        [Range(0f, 1f)]
        public float hpRatioThreshold;

        [Tooltip("Bu fazda kaç kere vursun.")]
        [Min(1)]
        public int hitCount;
    }

    [Header("HP Fazları (ALT eşikler)")]
    [Tooltip("Örn: 0.7→2, 0.5→3, 0.3→4, 0.1→5 gibi.\n" +
             "HP bu oranların ALTINA düştükçe hit sayısı artar.")]
    public HpPhase[] phases =
    {
        new HpPhase { hpRatioThreshold = 0.70f, hitCount = 2 },
        new HpPhase { hpRatioThreshold = 0.50f, hitCount = 3 },
        new HpPhase { hpRatioThreshold = 0.30f, hitCount = 4 },
        new HpPhase { hpRatioThreshold = 0.10f, hitCount = 5 },
    };

    [Header("Varsayılan")]
    [Tooltip("Hiçbir faza girmiyorsa (HP tüm threshold'lardan yüksekse) kaç kere vursun?")]
    [Min(1)]
    public int defaultHitCountAtFullHp = 1;

    [Header("Debug")]
    public bool logPhaseSelection = true;

    public override IEnumerator ExecuteAttackCoroutine(
        IAnimationBridge anim,
        CombatContext ctx,
        SimpleCombatant enemy,
        int baseAttackValue,
        EnemyAttackContextInfo info
    )
    {
        if (ctx == null || anim == null || enemy == null)
            yield break;

        var player = ctx.Player;
        if (player == null)
            yield break;

        // Resolution’dan gelen effective damage:
        int dmgPerHit = Mathf.Max(0, baseAttackValue);
        if (dmgPerHit <= 0)
        {
            if (logPhaseSelection)
                Debug.Log("[HpPhaseMultiHitBehaviour] baseAttackValue <= 0 → tümü blocklandı, hasar yok.");
            yield break;
        }

        // ----- HP oranını bul -----
        int currentHp = 1;
        int maxHp     = 1;

        var hm = enemy.GetComponentInChildren<HealthManager>(true) ?? enemy.GetComponent<HealthManager>();
        if (hm != null)
        {
            currentHp = Mathf.Max(0, hm.CurrentHP);
            maxHp     = Mathf.Max(1, hm.MaxHP);
        }

        float hpRatio = (float)currentHp / maxHp;

        // ----- Hit sayısını seç -----
        int hits = defaultHitCountAtFullHp;
        float usedThreshold = -1f;

        if (phases != null && phases.Length > 0)
        {
            // Büyükten küçüğe sırala: 0.7, 0.5, 0.3, 0.1 ...
            var sorted = phases.OrderByDescending(p => p.hpRatioThreshold).ToArray();

            // Mantık:
            // hits = default
            // for each threshold DESC:
            //   if hp <= threshold → hits = phase.hitCount
            //   else break;
            foreach (var phase in sorted)
            {
                if (hpRatio <= phase.hpRatioThreshold)
                {
                    hits = Mathf.Max(1, phase.hitCount);
                    usedThreshold = phase.hpRatioThreshold;
                }
                else
                {
                    // Daha yüksek bir threshold'u artık geçemedi, aşağıdakilere bakmaya gerek yok.
                    break;
                }
            }
        }

        if (logPhaseSelection)
        {
            Debug.Log(
                $"[HpPhaseMultiHitBehaviour] HP={currentHp}/{maxHp} ({hpRatio:0.00}) → " +
                $"hits={hits} (threshold={usedThreshold:0.00}, turn={info.turnIndex}, enemyRound={info.attackRoundIndex})"
            );
        }

        // ----- Seçilen sayıda vuruş yap -----
        for (int i = 0; i < hits; i++)
        {
            int hitIndex = i; // closure için

            yield return anim.PlayAttackAnimation(enemy, player, dmgPerHit, () =>
            {
                player.TakeDamage(dmgPerHit);

                if (logPhaseSelection)
                {
                    Debug.Log(
                        $"[HpPhaseMultiHitBehaviour] {enemy.name} hit #{hitIndex + 1}/{hits} " +
                        $"for {dmgPerHit} dmg (HPPhase, turn={info.turnIndex}, enemyRound={info.attackRoundIndex})."
                    );
                }
            });
        }
    }
}
