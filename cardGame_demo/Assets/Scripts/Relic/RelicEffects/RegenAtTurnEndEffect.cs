using System;
using UnityEngine;

[Serializable]
public class RegenAtTurnEndEffect : IRelicEffect
{
    [Min(1)] public int healPerStack = 5;
    public bool healPlayer  = true;
    public bool healEnemies = false;

    public void OnAcquire (RelicRuntime r, RelicContext c)
        => c?.combatDirector?.Log($"[{r.def.displayName}] Her tur sonunda +{healPerStack} HP (x{Mathf.Max(1,r.stacks)}).");

    public void OnLose (RelicRuntime r, RelicContext c)
        => c?.combatDirector?.Log($"[{r.def.displayName}] Regen pasifleşti.");

    // Turn-end: yeni turdan hemen önce
    public void OnTurnEnd(RelicRuntime r, RelicContext c)
    {
        int amount = healPerStack * Mathf.Max(1, r.stacks);

        if (healPlayer && c?.player)
            c.player.GetComponent<HealthManager>()?.Heal(amount);

        if (healEnemies && c?.combatDirector)
        {
            // Basit: sahnedeki tüm düşmanları bul
            var all = GameObject.FindObjectsOfType<SimpleCombatant>();
            foreach (var sc in all)
            {
                if (sc == c.player) continue;
                sc.GetComponent<HealthManager>()?.Heal(amount);
            }
        }
    }

    // Diğer kancalar/hatlar dokunma
    public void OnTurnStart(RelicRuntime r, RelicContext c) {}
    public void OnShuffle(RelicRuntime r, RelicContext c) {}
    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawn) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}
    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float v, ref bool a) => v;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float v, ref bool a) => v;
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float v, ref bool a) => v;
    public int   ModifyDrawCount  (RelicRuntime r, RelicContext c, int v, ref bool a) => v;
    public int   ModifyEnergyGain (RelicRuntime r, RelicContext c, int v, ref bool a) => v;
    public float ModifyStat(RelicRuntime r, RelicContext c, StatId s, float v, ref bool a) => v;
    public int   ModifyAttackThreshold (RelicRuntime r, RelicContext c, int v, ref bool a) => v;
    public int   ModifyDefenseThreshold(RelicRuntime r, RelicContext c, int v, ref bool a) => v;
}
