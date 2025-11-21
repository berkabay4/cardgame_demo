using UnityEngine;
using System.Collections;

[CreateAssetMenu(
    menuName = "CardGame/Enemy/Elite Enemy/MiniBoss Behaviours/Basic Multi Hit",
    fileName = "BasicMultiHitBehaviour")]
public class BasicMultiHitBehaviour : MiniBossAttackBehaviour
{
    [Tooltip("Çift ellerde kaç kere vurulsun (ör: 3 = 3x5 dmg).")]
    public int multiHitCountOnEvenTurn = 3;

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

        // Kaç hit atılacak?
        int hits = 1;

        // PATTERN:
        //  - 1. el (turnIndex = 1) → 1 hit
        //  - 2. el (turnIndex = 2) → multiHitCountOnEvenTurn hit
        //  - 3. el → yine 1 hit
        //  - 4. el → multiHitCountOnEvenTurn hit
        if (info.turnIndex % 2 == 0)
            hits = Mathf.Max(1, multiHitCountOnEvenTurn);

        int dmgPerHit = Mathf.Max(0, baseAttackValue);
        if (dmgPerHit <= 0)
        {
            Debug.Log("[MiniBossBehaviour] baseAttackValue <= 0, saldırı yapılmadı.");
            yield break;
        }

        for (int i = 0; i < hits; i++)
        {
            int hitIndex = i; // closure için

            yield return anim.PlayAttackAnimation(enemy, player, dmgPerHit, () =>
            {
                player.TakeDamage(dmgPerHit);
                Debug.Log(
                    $"[MiniBoss] {enemy.name} hit #{hitIndex + 1}/{hits} " +
                    $"for {dmgPerHit} (Turn={info.turnIndex}, EnemyRound={info.attackRoundIndex}, Fight={info.fightKind})."
                );
            });
        }
    }
}
