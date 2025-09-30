using System;
using UnityEngine;

[Serializable]
public class DamageEnemiesOnTurnStartEffect : IRelicEffect
{
    [Min(0)] public int damagePerStack = 2;
    public bool skipDeadTargets = true;   // HP<=0 düşmanları atla
    public bool logEachHit = false;       // her hedef için log

    // === lifecycle ===
    public void OnAcquire(RelicRuntime r, RelicContext c)
        => c?.director?.Log($"[{r.def.displayName}] Her oyuncu turu başında düşmanlara {damagePerStack} hasar (x{Mathf.Max(1,r.stacks)}).");

    public void OnLose(RelicRuntime r, RelicContext c)
        => c?.director?.Log($"[{r?.def?.displayName}] Tur başı hasar etkisi kaldırıldı.");

    // === hooks ===
    public void OnTurnStart(RelicRuntime r, RelicContext c)
    {
        // GameDirector.StartNewTurn() → RelicManager.OnTurnStart() çağırıyor
        // yani burası HER oyuncu turu başında tetikleniyor.
        if (c?.director == null) return;

        int dmg = damagePerStack * Mathf.Max(1, r.stacks);
        if (dmg <= 0) return;

        var all = GameObject.FindObjectsOfType<SimpleCombatant>();
        foreach (var sc in all)
        {
            if (sc == c.player) continue;

            // HealthManager varsa kullan
            var hm = sc.GetComponent<HealthManager>();
            if (skipDeadTargets && hm != null && hm.CurrentHP <= 0) continue;

            if (hm != null) hm.TakeDamage(dmg);
            else            sc.TakeDamage(dmg);

            if (logEachHit) c.director.Log($"[{r.def.displayName}] {sc.name} -{dmg} (turn start).");
        }
    }

    public void OnTurnEnd  (RelicRuntime r, RelicContext c) {}
    public void OnShuffle  (RelicRuntime r, RelicContext c) {}
    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawn) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}

    // === pipelines (no-op) ===
    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public int   ModifyDrawCount  (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;
    public int   ModifyEnergyGain (RelicRuntime r, RelicContext c, int baseValue, ref bool applied)    => baseValue;
    public float ModifyStat       (RelicRuntime r, RelicContext c, StatId stat, float baseValue, ref bool applied) => baseValue;
    public int   ModifyAttackThreshold (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int   ModifyDefenseThreshold(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
}
