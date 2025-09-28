using System;
using UnityEngine;
using System.Collections.Generic;

public interface IRelicEffect
{
    // Yaşam döngüsü
    void OnAcquire(RelicRuntime runtime, RelicContext ctx);
    void OnLose(RelicRuntime runtime, RelicContext ctx);
    public float ModifyAttackValue (RelicRuntime r, RelicContext c, float baseValue, ref bool applied)  => baseValue;
    public float ModifyDefenseValue(RelicRuntime r, RelicContext c, float baseValue, ref bool applied)  => baseValue;

    // Kancalar (gerekli olanları boş bırakılabilir)
    void OnTurnStart(RelicRuntime runtime, RelicContext ctx);
    void OnTurnEnd(RelicRuntime runtime, RelicContext ctx);
    void OnShuffle(RelicRuntime runtime, RelicContext ctx);

    void OnCardDrawn(RelicRuntime runtime, RelicContext ctx, Card drawnCard);
    void OnCardPlayed(RelicRuntime runtime, RelicContext ctx, Card playedCard);

    // Değer değiştiriciler (pipeline)
    float ModifyDamageDealt(RelicRuntime runtime, RelicContext ctx, float baseValue, ref bool applied);
    int   ModifyDrawCount(RelicRuntime runtime, RelicContext ctx, int baseValue, ref bool applied);
    int   ModifyEnergyGain(RelicRuntime runtime, RelicContext ctx, int baseValue, ref bool applied);
}

// Runtime tarafında stack, geçici sayaç vb.
[Serializable]
public class RelicRuntime
{
    public RelicDefinition def;
    public int stacks = 1;
    public bool isEnabled = true;

    // Efektlerin iç kullanımı için değişken alanı (ör. sayaçlar)
    [NonSerialized] public object userData;
}

public class RelicContext
{
    // Zorunlu bağlam
    public GameDirector director;
    public SimpleCombatant player;
    public SimpleCombatant enemy;   // hedef düşman (isteğe bağlı)

    // Akış bilgisi
    public TurnStep step;

    // === BattleState'ten gelenler ===
    public int playerAtkTotal;
    public int playerDefTotal;

    public bool waitingForTarget;
    public SimpleCombatant currentTarget;

    public IReadOnlyDictionary<SimpleCombatant,int> enemyAtkTotals;
    public IReadOnlyDictionary<SimpleCombatant,int> enemyDefTotals;

    // === Eski/opsiyonel alanlar (bazı relic effect'leri kullanabilir) ===
    public int turnNumber;      // sende yok → 0 bırakıyoruz
    public int deckCount;       // sende yok → 0 bırakıyoruz
    public int discardCount;    // sende yok → 0 bırakıyoruz
    public int handCount;       // sende yok → 0 bırakıyoruz
    public int energyThisTurn;  // sende yok → 0 bırakıyoruz

    public RelicContext(GameDirector dir, SimpleCombatant p, SimpleCombatant e)
    {
        director = dir;
        player = p;
        enemy  = e;
    }
}

