using System;
using UnityEngine;

[Serializable]
public class AttackRangeBonusEffect : IRelicEffect
{
    [Min(1)] public int  flatBonusPerStack = 5; // her stack +5
    public bool applyToPlayer = true;           // oyuncuya uygula
    public bool applyToEnemies = false;         // istersen düşmanlara da aç
    public bool onlyOnAttackPhase = true;       // sadece Attack eşiğinde/bağlamında

    // ===== lifecycle (log) =====
    public void OnAcquire(RelicRuntime r, RelicContext c)
    {
        if (c?.combatDirector != null)
            c.combatDirector.Log($"[{r.def.displayName}] +AttackThreshold: +{flatBonusPerStack} x{Mathf.Max(1, r.stacks)}");
    }
    public void OnLose(RelicRuntime r, RelicContext c)
    {
        c?.combatDirector?.Log($"[{r.def.displayName}] AttackThreshold bonusu kaldırıldı.");
    }

    public void OnTurnStart(RelicRuntime r, RelicContext c) {}
    public void OnTurnEnd  (RelicRuntime r, RelicContext c) {}
    public void OnShuffle  (RelicRuntime r, RelicContext c) {}
    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawn) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}

    // ===== value pipelines (dokunmuyor) =====
    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public int   ModifyDrawCount  (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;
    public int   ModifyEnergyGain (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;

    // ===== genel stat pipeline =====
    public float ModifyStat(RelicRuntime r, RelicContext c, StatId stat, float baseValue, ref bool applied)
    {
        if (!r.isEnabled) return baseValue;
        if (stat != StatId.AttackThreshold) return baseValue;
        if (!ShouldApplyToContext(c)) return baseValue;

        int delta = flatBonusPerStack * Mathf.Max(1, r.stacks);
        applied = true;
        return baseValue + delta;
    }

    // ===== threshold (geriye dönük uyumluluk) =====
    public int ModifyAttackThreshold(RelicRuntime r, RelicContext c, int baseValue, ref bool applied)
    {
        float v = ModifyStat(r, c, StatId.AttackThreshold, baseValue, ref applied);
        return Mathf.RoundToInt(v);
    }

    public int ModifyDefenseThreshold(RelicRuntime r, RelicContext c, int baseValue, ref bool applied)
        => baseValue;

    // ===== helpers =====
    bool ShouldApplyToContext(RelicContext c)
    {
        if (c == null) return true; // güvenli varsayım

        // Hangi aktöre?
        bool actorOk =
            (applyToPlayer  && c.thresholdForActor == Actor.Player) ||
            (applyToEnemies && c.thresholdForActor == Actor.Enemy);

        if (!actorOk) return false;

        // Hangi faz/bağlam?
        if (onlyOnAttackPhase && c.thresholdForPhase != PhaseKind.Attack)
            return false;

        return true;
    }
}
