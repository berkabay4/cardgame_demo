using System;
using UnityEngine;

[Serializable]
public class PingEnemiesOnDrawEffect : IRelicEffect
{
    [Min(0)] public int damagePerStack = 1;
    public bool onlyOnPlayerPhases = true; // PlayerDef/PlayerAtk sırasında

    // === lifecycle ===
    public void OnAcquire(RelicRuntime r, RelicContext c)
        => c?.combatDirector?.Log($"[{r.def.displayName}] Kart çekiminde düşmanlara {damagePerStack} hasar (x{Mathf.Max(1, r.stacks)}).");

    public void OnLose(RelicRuntime r, RelicContext c)
        => c?.combatDirector?.Log($"[{r?.def?.displayName}] Kart çekiminde hasar etkisi kaldırıldı.");

    // === hooks ===
    public void OnTurnStart(RelicRuntime r, RelicContext c) {}
    public void OnTurnEnd  (RelicRuntime r, RelicContext c) {}
    public void OnShuffle  (RelicRuntime r, RelicContext c) {}

    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawn)
    {
        if (onlyOnPlayerPhases)
        {
            if (c == null) return;
            if (c.step != TurnStep.PlayerDef && c.step != TurnStep.PlayerAtk) return;
        }

        int dmg = damagePerStack * Mathf.Max(1, r.stacks);
        if (dmg <= 0 || c?.combatDirector == null) return;

        // Basit yol: Player dışındaki tüm SimpleCombatant'lara hasar ver
        var all = GameObject.FindObjectsOfType<SimpleCombatant>();
        foreach (var sc in all)
        {
            if (sc == c.player) continue;
            sc.TakeDamage(dmg);
        }
    }

    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}

    // === pipelines (no-op) ===
    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public int   ModifyDrawCount  (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;
    public int   ModifyEnergyGain (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;

    // Genel stat hattı (kullanılmıyor)
    public float ModifyStat(RelicRuntime r, RelicContext c, StatId stat, float baseValue, ref bool applied) => baseValue;

    // Threshold geri uyumluluk (kullanılmıyor)
    public int ModifyAttackThreshold (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int ModifyDefenseThreshold(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
}
