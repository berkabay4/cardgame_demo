using System;
using UnityEngine;

[Serializable]
public class AttackRangeBonusEffect : IRelicEffect
{
    [Min(1)] public int flatBonusPerStack = 5;  // her stack başına +5
    public bool applyToPlayer = true;           // ileride düşmanlara da açmak istersen
    public bool onlyOnPlayerTurn = true;        // sadece PlayerAtk adımında uygula

    // === lifecycle ===
    public void OnAcquire (RelicRuntime r, RelicContext c)
    {
        c.director?.Log($"[{r.def.displayName}] Attack range bonusu aktif (+{flatBonusPerStack} x{Mathf.Max(1,r.stacks)}).");
    }

    public void OnLose (RelicRuntime r, RelicContext c)
    {
        c.director?.Log($"[{r.def.displayName}] Attack range bonusu kaldırıldı.");
    }

    public void OnTurnStart(RelicRuntime r, RelicContext c) {}
    public void OnTurnEnd  (RelicRuntime r, RelicContext c) {}
    public void OnShuffle  (RelicRuntime r, RelicContext c) {}
    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawn) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}

    // === pipelines ===
    // Not: "AttackRange bonusu"nu, saldırı toplamının efektif değerine FLAT ekleme olarak yorumladık.
    // Eğer gerçek hedefin "threshold"ü artırmaksa, CombatContext tarafında ayrı bir modifier hattı açmanı öneririm.
    public float ModifyAttackValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)
    {
        if (!r.isEnabled) return baseValue;
        if (onlyOnPlayerTurn && c.step != TurnStep.PlayerAtk) return baseValue;

        // Şimdilik sadece oyuncuya uygula (ileride düşman için genişletebilirsin)
        if (!applyToPlayer) return baseValue;

        applied = true;
        int delta = flatBonusPerStack * Mathf.Max(1, r.stacks);
        return baseValue + delta;
    }

    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public int   ModifyDrawCount  (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int   ModifyEnergyGain (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
}
