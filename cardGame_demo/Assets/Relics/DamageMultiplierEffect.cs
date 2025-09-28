using UnityEngine;

[System.Serializable]
public class DamageMultiplierEffect : IRelicEffect
{
    [Range(0f, 5f)] public float multiplier = 1.10f;  // ör: +%10
    [Header("Apply To")]
    public bool applyOnAttack = true;                  // elde üretilen ATT değerine uygula
    public bool applyOnDefense = false;                // elde üretilen DEF değerine uygula

    [Header("Whose Turn?")]
    public bool onlyOnPlayerTurn = true;               // sadece oyuncu turunda
    public bool onlyOnEnemyTurn  = false;              // sadece düşman turunda

    // ==== Lifecycle/other hooks (boş bırakılabilir) ====
    public void OnAcquire   (RelicRuntime r, RelicContext c) {}
    public void OnLose      (RelicRuntime r, RelicContext c) {}
    public void OnTurnStart (RelicRuntime r, RelicContext c) {}
    public void OnTurnEnd   (RelicRuntime r, RelicContext c) {}
    public void OnShuffle   (RelicRuntime r, RelicContext c) {}
    public void OnCardDrawn (RelicRuntime r, RelicContext c, Card drawn) {}
    public void OnCardPlayed(RelicRuntime r, RelicContext c, Card played) {}

    // ==== Outgoing damage (klasik saldırı kartları için; sende istersen kullanmaya devam edebilirsin) ====
    public float ModifyDamageDealt(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)
    {
        // Bu oyunda şart değil; ister aktif tut ister sadece ATT/DEF kancalarını kullan
        if (!r.isEnabled) return baseValue;
        if (!PassesTurnFilter(c)) return baseValue;

        applied = true;
        return baseValue * Mathf.Pow(multiplier, r.stacks);
    }

    // ==== YENİ: elde üretilen ATT ====
    public float ModifyAttackValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)
    {
        if (!r.isEnabled || !applyOnAttack) return baseValue;
        if (!PassesTurnFilter(c)) return baseValue;

        applied = true;
        return baseValue * Mathf.Pow(multiplier, r.stacks);
    }

    // ==== YENİ: elde üretilen DEF ====
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)
    {
        if (!r.isEnabled || !applyOnDefense) return baseValue;
        if (!PassesTurnFilter(c)) return baseValue;

        applied = true;
        return baseValue * Mathf.Pow(multiplier, r.stacks);
    }

    // Enerji sistemin yoksa bu zaten etkisiz kalır
    public int ModifyDrawCount(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;
    public int ModifyEnergyGain(RelicRuntime r, RelicContext c, int baseValue, ref bool applied) => baseValue;

    // --- yardımcı ---
    private bool PassesTurnFilter(RelicContext c)
    {
        // TurnStep: PlayerDef, PlayerAtk, EnemyDef, EnemyAtk, Resolve (sende bu şekildeydi)
        bool isPlayerTurn = (c.step == TurnStep.PlayerDef || c.step == TurnStep.PlayerAtk);
        bool isEnemyTurn  = (c.step == TurnStep.EnemyDef  || c.step == TurnStep.EnemyAtk);

        if (onlyOnPlayerTurn && !isPlayerTurn) return false;
        if (onlyOnEnemyTurn  && !isEnemyTurn)  return false;

        return true;
    }
}
