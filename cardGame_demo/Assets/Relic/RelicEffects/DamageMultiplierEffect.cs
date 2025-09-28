using System;
using UnityEngine;

[Serializable]
public class DamageMultiplierEffect : IRelicEffect
{
    [Range(0f, 5f)] public float multiplier = 1.10f; // her stack için çarpan değil; toplamda pow(multiplier, stacks)
    public bool onlyOnPlayerTurn = true;             // isterse sadece PlayerAtk adımında çalışsın

    // === lifecycle ===
    public void OnAcquire(RelicRuntime r, RelicContext c) {}
    public void OnLose   (RelicRuntime r, RelicContext c) {}

    public void OnTurnStart(RelicRuntime r, RelicContext c) {}
    public void OnTurnEnd  (RelicRuntime r, RelicContext c) {}
    public void OnShuffle  (RelicRuntime r, RelicContext c) {}

    public void OnCardDrawn(RelicRuntime r, RelicContext c, Card drawnCard) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card playedCard) {}

    // === pipelines ===
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)
    {
        if (!r.isEnabled) return baseValue;
        if (onlyOnPlayerTurn && c.step != TurnStep.PlayerAtk) return baseValue;

        applied = true;
        var stacks = Mathf.Max(1, r.stacks);
        return baseValue * Mathf.Pow(multiplier, stacks);
    }

    public int   ModifyDrawCount (RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int   ModifyEnergyGain(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;

    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied) => baseValue;
}
