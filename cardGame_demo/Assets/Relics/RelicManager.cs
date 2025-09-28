using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [SerializeField] private GameDirector director;
    [SerializeField] private List<RelicRuntime> relics = new();

    // Sıra: additive -> multiplicative -> clamp (ihtiyaç olursa)
    [Header("Modifier Order")]
    [SerializeField] private List<string> priorityOrder = new() {
        "FlatDamageUp", "ConditionalAdds", "Multipliers", "LastMinuteClamps"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!director) director = GameDirector.Instance ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
    }

    public IEnumerable<RelicRuntime> All => relics;

    public bool Has(string relicId) => relics.Any(r => r.def.relicId == relicId);

    public RelicRuntime Get(string relicId) => relics.FirstOrDefault(r => r.def.relicId == relicId);

    public void Acquire(RelicDefinition def, int stacks = 1)
    {
        var ctx = BuildContext();
        var existing = Get(def.relicId);
        if (existing != null)
        {
            switch (def.stackRule)
            {
                case RelicStackRule.Unique:
                    director.Log($"[{def.displayName}] zaten var (Unique).");
                    return;
                case RelicStackRule.Stackable:
                    existing.stacks = Mathf.Clamp(existing.stacks + stacks, 1, def.maxStacks);
                    director.Log($"[{def.displayName}] stack oldu: {existing.stacks}.");
                    break;
                case RelicStackRule.ReplaceLower:
                    existing.stacks = Mathf.Max(existing.stacks, stacks);
                    break;
                case RelicStackRule.ReplaceHigher:
                    existing.stacks = Mathf.Min(existing.stacks, stacks);
                    break;
            }
            foreach (var e in def.effects) e.OnAcquire(existing, ctx); // yeniden tetiklemek isteyebilirsiniz
        }
        else
        {
            var rt = new RelicRuntime { def = def, stacks = Mathf.Clamp(stacks,1,def.maxStacks), isEnabled = true };
            relics.Add(rt);
            foreach (var e in def.effects) e.OnAcquire(rt, ctx);
            director.Log($"+ Relic: {def.displayName}");
            // UI toast vs.
        }
        // UI’yi güncelle
        director.Events?.OnRelicsChanged?.Invoke();
    }
    public float ApplyAttackValueModifiers(float baseValue)
    {
        float v = baseValue;
        var ctx = BuildContext();
        foreach (var r in relics)
            foreach (var e in r.def.effects)
            {
                bool applied = false;
                v = e.ModifyAttackValue(r, ctx, v, ref applied);
            }
        return v;
    }

    public float ApplyDefenseValueModifiers(float baseValue)
    {
        float v = baseValue;
        var ctx = BuildContext();
        foreach (var r in relics)
            foreach (var e in r.def.effects)
            {
                bool applied = false;
                v = e.ModifyDefenseValue(r, ctx, v, ref applied);
            }
        return v;
    }
    public void Lose(string relicId)
    {
        var rt = Get(relicId);
        if (rt == null) return;
        var ctx = BuildContext();
        foreach (var e in rt.def.effects) e.OnLose(rt, ctx);
        relics.Remove(rt);
        director.Events?.OnRelicsChanged?.Invoke();
    }

    public RelicContext BuildContext(SimpleCombatant targetEnemy = null)
    {
        var ctx = new RelicContext(director, director.Player, targetEnemy);

        var st = director.State; // BattleState
        if (st != null)
        {
            ctx.step = st.Step;

            ctx.playerAtkTotal = st.PlayerAtkTotal;
            ctx.playerDefTotal = st.PlayerDefTotal;

            ctx.waitingForTarget = st.WaitingForTarget;
            ctx.currentTarget    = st.CurrentTarget;

            // Dictionary'leri ReadOnly olarak bağlamak için doğrudan referans veriyoruz
            ctx.enemyAtkTotals = st.EnemyAtkTotals;
            ctx.enemyDefTotals = st.EnemyDefTotals;

            // Opsiyoneller (sende yok → 0)
            ctx.turnNumber    = 0;
            ctx.deckCount     = 0;
            ctx.discardCount  = 0;
            ctx.handCount     = 0;
            ctx.energyThisTurn = 0;
        }
        else
        {
            // State yoksa güvenli varsayılanlar
            ctx.step = TurnStep.PlayerAtk;
            ctx.playerAtkTotal = 0;
            ctx.playerDefTotal = 0;
            ctx.waitingForTarget = false;
            ctx.currentTarget = null;
            ctx.enemyAtkTotals = null;
            ctx.enemyDefTotals = null;
            ctx.turnNumber = ctx.deckCount = ctx.discardCount = ctx.handCount = ctx.energyThisTurn = 0;
        }

        return ctx;
    }

    // ==== Hook relay ====
    public void OnTurnStart() { var ctx = BuildContext(); foreach (var r in relics) foreach (var e in r.def.effects) e.OnTurnStart(r, ctx); }
    public void OnTurnEnd()   { var ctx = BuildContext(); foreach (var r in relics) foreach (var e in r.def.effects) e.OnTurnEnd(r, ctx); }
    public void OnShuffle()   { var ctx = BuildContext(); foreach (var r in relics) foreach (var e in r.def.effects) e.OnShuffle(r, ctx); }
    public void OnCardDrawn(Card c) { var ctx = BuildContext(); foreach (var r in relics) foreach (var e in r.def.effects) e.OnCardDrawn(r, ctx, c); }
    public void OnCardPlayed(Card c){ var ctx = BuildContext(); foreach (var r in relics) foreach (var e in r.def.effects) e.OnCardPlayed(r, ctx, c); }

    // ==== Modifier pipeline ====
    public float ApplyDamageModifiers(float baseValue)
    {
        float v = baseValue;
        var ctx = BuildContext();
        foreach (var r in relics)
            foreach (var e in r.def.effects)
            {
                bool applied = false;
                v = e.ModifyDamageDealt(r, ctx, v, ref applied);
            }
        return v;
    }

    public int ApplyDrawCountModifiers(int baseValue)
    {
        int v = baseValue;
        var ctx = BuildContext();
        foreach (var r in relics)
            foreach (var e in r.def.effects)
            {
                bool applied = false;
                v = e.ModifyDrawCount(r, ctx, v, ref applied);
            }
        return Mathf.Max(0, v);
    }

    public int ApplyEnergyGainModifiers(int baseValue)
    {
        int v = baseValue;
        var ctx = BuildContext();
        foreach (var r in relics)
            foreach (var e in r.def.effects)
            {
                bool applied = false;
                v = e.ModifyEnergyGain(r, ctx, v, ref applied);
            }
        return Mathf.Max(0, v);
    }
}
