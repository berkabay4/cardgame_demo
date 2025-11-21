using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class MiniBossRuntime : MonoBehaviour
{
    [SerializeField] private MiniBossDefinition definition;
    public MiniBossDefinition Definition => definition;

    /// <summary>Bilgi amaçlı; gerçek HP HealthManager üzerinden tutulur.</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>Combat’a spawn olurken EliteEnemyDataApplier tarafından çağrılır.</summary>
    public void Init(MiniBossDefinition def)
    {
        definition = def;

        int maxHp = (def != null) ? Mathf.Max(1, def.maxHealth) : 1;

        // HealthManager ile senkronize et
        var hm = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.SetMaxHP(maxHp, keepRatio: false);
            hm.RefillToMax();
            CurrentHealth = hm.CurrentHP;
        }
        else
        {
            CurrentHealth = maxHp;
        }
    }

    /// <summary>
    /// Eski API ile uyum için bırakılmıştır. Mümkünse ResolutionController üzerinden kullan.
    /// </summary>
    public void TakeTurn(CombatContext ctx, int baseAttackValue, int attackRoundIndex)
    {
        if (definition == null || definition.attackBehaviour == null || ctx == null)
            return;

        var director = CombatDirector.Instance;
        var anim = director as IAnimationBridge;
        if (anim == null)
        {
            Debug.LogWarning("[MiniBossRuntime] No IAnimationBridge found on CombatDirector.");
            return;
        }

        var sc = GetComponent<SimpleCombatant>();
        if (sc == null) return;

        var info = new EnemyAttackContextInfo
        {
            fightKind = director != null ? (EnemyFightKind)director.CurrentFightKind : EnemyFightKind.EliteMiniBoss,
            turnIndex = director != null ? director.TurnIndex : 1,
            attackRoundIndex = attackRoundIndex
        };

        director.Run(definition.attackBehaviour.ExecuteAttackCoroutine(anim, ctx, sc, baseAttackValue, info));
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        var hm = GetComponentInChildren<HealthManager>(true) ?? GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.TakeDamage(amount);
            CurrentHealth = hm.CurrentHP;
        }
        else
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        }

        // Ölüm animasyonu vs. buraya.
    }
}
