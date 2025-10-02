using System;
using UnityEngine;

[Serializable]
public class MaxHealthBonusEffect : IRelicEffect
{
    public enum Mode { Flat, Percent }

    [Header("Settings")]
    public Mode mode = Mode.Flat;

    [Min(0)] public int   flatBonusPerStack    = 10;   // Flat: her stack +10 MaxHP
    [Range(0f, 5f)] public float percentPerStack = 0.10f; // Percent: her stack +%10 MaxHP

    [Header("Targeting")]
    public bool applyToPlayer  = true;
    public bool applyToEnemies = false;

    // ===== lifecycle (log) =====
    public void OnAcquire(RelicRuntime r, RelicContext c)
    {
        if (c?.combatDirector == null || r?.def == null) return;

        string txt = mode == Mode.Flat
            ? $"+MaxHP: +{flatBonusPerStack} x{Mathf.Max(1,r.stacks)}"
            : $"+MaxHP: +{percentPerStack*100f:0.#}% x{Mathf.Max(1,r.stacks)}";

        c.combatDirector.Log($"[{r.def.displayName}] {txt}");
    }

    public void OnLose(RelicRuntime r, RelicContext c)
    {
        c?.combatDirector?.Log($"[{r?.def?.displayName}] MaxHP bonusu kaldırıldı.");
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
        if (stat != StatId.MaxHealth) return baseValue;
        if (!ShouldApplyToContext(c)) return baseValue;

        int stacks = Mathf.Max(1, r.stacks);

        switch (mode)
        {
            case Mode.Flat:
            {
                float v = baseValue + (flatBonusPerStack * stacks);
                applied = true;
                return Mathf.Max(1f, v);
            }

            case Mode.Percent:
            {
                // toplam çarpan: (1 + p)^stacks (istersen lineer: 1 + p*stacks yapabilirsin)
                float multiplier = Mathf.Pow(1f + Mathf.Max(0f, percentPerStack), stacks);
                float v = baseValue * multiplier;
                applied = true;
                return Mathf.Max(1f, v);
            }
        }

        return baseValue;
    }

    // ===== threshold (geriye dönük uyumluluk; kullanmıyoruz) =====
    public int ModifyAttackThreshold (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int ModifyDefenseThreshold(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;

    // ===== helpers =====
    bool ShouldApplyToContext(RelicContext c)
    {
        // Actor filtresi: RelicManager, ctx.thresholdForActor'ı dolduruyor (MaxHealth için de)
        if (c != null)
        {
            bool actorOk =
                (applyToPlayer  && c.thresholdForActor == Actor.Player) ||
                (applyToEnemies && c.thresholdForActor == Actor.Enemy);

            if (!(applyToPlayer || applyToEnemies)) actorOk = true; // hedef seçilmediyse herkese uygula
            if (!actorOk) return false;
        }
        return true;
    }
}
