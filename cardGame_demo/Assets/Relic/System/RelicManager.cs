using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class RelicManager : MonoBehaviour
{
    // ==== Singleton ====
    public static RelicManager Instance { get; private set; }

    // ==== Events ====
    /// Envanter değiştiğinde tetiklenir (Acquire/Lose/Enable/Disable/SetStacks/Clear).
    public event Action OnRelicsChanged;

    /// UI köprüsü ile birlikte tek noktadan bildirim
    public void RaiseRelicsChanged()
    {
        OnRelicsChanged?.Invoke();
        if (director?.Events != null)
            director.Events.OnRelicsChanged?.Invoke();
    }

    // ==== Refs / Data ====
    [SerializeField] private GameDirector director;
    [SerializeField] private List<RelicRuntime> relics = new();

    [Header("Modifier Order (reserved)")]
    [SerializeField] private List<string> priorityOrder = new()
    {
        "FlatDamageUp", "ConditionalAdds", "Multipliers", "LastMinuteClamps"
    };

    // ==== Lifecycle ====
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResolveDirectorIfNull();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) ResolveDirectorIfNull();
    }

    private void ResolveDirectorIfNull()
    {
        if (!director)
            director = GameDirector.Instance ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
    }

    // ==== Queries / Accessors ====
    public IEnumerable<RelicRuntime> All => relics ?? Enumerable.Empty<RelicRuntime>();
    public int Count => relics?.Count ?? 0;

    public bool Has(string relicId) => relics.Any(r => r?.def && r.def.relicId == relicId);

    public RelicRuntime Get(string relicId) =>
        relics.FirstOrDefault(r => r?.def && r.def.relicId == relicId);

    public bool TryGet(string relicId, out RelicRuntime runtime)
    {
        runtime = Get(relicId);
        return runtime != null;
    }

    // ==== Mutations ====
    public void Acquire(RelicDefinition def, int stacks = 1)
    {
        if (!def) { director?.Log("[Relic] Acquire: null definition."); return; }

        var existing = Get(def.relicId);
        if (existing != null && existing.def != def)
        {
            Debug.LogWarning($"[Relic] Duplicate relicId '{def.relicId}' across different assets: " +
                            $"existing='{existing.def.name}', new='{def.name}'. " +
                            $"Stacking will affect the EXISTING one.");
        }


        var ctx = BuildContext();

        if (existing != null)
        {
            switch (def.stackRule)
            {
                case RelicStackRule.Unique:
                    director?.Log($"[{def.displayName}] zaten var (Unique).");
                    RaiseRelicsChanged();
                    return;

                case RelicStackRule.Stackable:
                    existing.stacks = Mathf.Clamp(existing.stacks + stacks, 1, def.maxStacks);
                    director?.Log($"[{def.displayName}] stack oldu: {existing.stacks}.");
                    break;

                case RelicStackRule.ReplaceLower:
                    existing.stacks = Mathf.Max(existing.stacks, stacks);
                    break;

                case RelicStackRule.ReplaceHigher:
                    existing.stacks = Mathf.Min(existing.stacks, stacks);
                    break;
            }

            if (def.effects != null)
                foreach (var e in def.effects) e?.OnAcquire(existing, ctx);
        }
        else
        {
            var rt = new RelicRuntime
            {
                def = def,
                stacks = Mathf.Clamp(stacks, 1, def.maxStacks),
                isEnabled = true
            };
            relics.Add(rt);

            if (def.effects != null)
                foreach (var e in def.effects) e?.OnAcquire(rt, ctx);

            director?.Log($"+ Relic: {def.displayName}");
        }

        RaiseRelicsChanged();
    }

    public void Lose(string relicId)
    {
        var rt = Get(relicId);
        if (rt == null) return;

        var ctx = BuildContext();
        if (rt.def?.effects != null)
            foreach (var e in rt.def.effects) e?.OnLose(rt, ctx);

        relics.Remove(rt);
        RaiseRelicsChanged();
    }

    public void ClearAll(bool callLoseHooks = true)
    {
        ResolveDirectorIfNull();
        var ctx = BuildContext();

        if (callLoseHooks)
        {
            foreach (var rt in relics)
                if (rt?.def?.effects != null)
                    foreach (var e in rt.def.effects) e?.OnLose(rt, ctx);
        }

        relics.Clear();
        RaiseRelicsChanged();
    }

    public void SetStacks(string relicId, int stacks)
    {
        var rt = Get(relicId);
        if (rt == null || rt.def == null) return;

        stacks = Mathf.Clamp(stacks, 1, rt.def.maxStacks);
        if (rt.stacks == stacks) return;

        rt.stacks = stacks;
        RaiseRelicsChanged();
    }

    public void SetEnabled(string relicId, bool enabled)
    {
        var rt = Get(relicId);
        if (rt == null) return;

        if (rt.isEnabled != enabled)
        {
            rt.isEnabled = enabled;
            RaiseRelicsChanged();
        }
    }

    // ==== Context ====
    public RelicContext BuildContext(SimpleCombatant targetEnemy = null)
    {
        ResolveDirectorIfNull();

        var player = director ? director.Player : null;
        var ctx = new RelicContext(director, player, targetEnemy);

        var st = director ? director.State : null;
        if (st != null)
        {
            ctx.step = st.Step;
            ctx.playerAtkTotal = st.PlayerAtkTotal;
            ctx.playerDefTotal = st.PlayerDefTotal;
            ctx.waitingForTarget = st.WaitingForTarget;
            ctx.currentTarget    = st.CurrentTarget;
            ctx.enemyAtkTotals   = st.EnemyAtkTotals;
            ctx.enemyDefTotals   = st.EnemyDefTotals;

            ctx.turnNumber = 0;
            ctx.deckCount = 0;
            ctx.discardCount = 0;
            ctx.handCount = 0;
            ctx.energyThisTurn = 0;
        }
        else
        {
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
    public void OnTurnStart()
    {
        var ctx = BuildContext();
        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects) e?.OnTurnStart(r, ctx);
    }

    public void OnTurnEnd()
    {
        var ctx = BuildContext();
        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects) e?.OnTurnEnd(r, ctx);
    }

    public void OnShuffle()
    {
        var ctx = BuildContext();
        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects) e?.OnShuffle(r, ctx);
    }

    public void OnCardDrawn(Card c)
    {
        var ctx = BuildContext();
        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects) e?.OnCardDrawn(r, ctx, c);
    }

    public void OnCardPlayed(Card c)
    {
        var ctx = BuildContext();
        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects) e?.OnCardPlayed(r, ctx, c);
    }

    // ==== Pipelines ====
    public float ApplyAttackValueModifiers(float baseValue)
    {
        var ctx = BuildContext();
        float v = baseValue;

        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects)
                {
                    bool applied = false;
                    v = e.ModifyAttackValue(r, ctx, v, ref applied);
                }
        return v;
    }

    public float ApplyDefenseValueModifiers(float baseValue)
    {
        var ctx = BuildContext();
        float v = baseValue;

        foreach (var r in relics)
            if (r?.isEnabled == true && r.def?.effects != null)
                foreach (var e in r.def.effects)
                {
                    bool applied = false;
                    v = e.ModifyDefenseValue(r, ctx, v, ref applied);
                }
        return v;
    }
    public float ApplyStatModifiers(StatId stat, float baseValue, Actor actor, PhaseKind phase)
    {
        var ctx = BuildContext();
        ctx.thresholdForActor = actor;
        ctx.thresholdForPhase = phase;

        float v = baseValue;

        foreach (var r in All)
        {
            if (r?.isEnabled != true || r.def?.effects == null) continue;

            foreach (var e in r.def.effects)
            {
                bool applied = false;

                if (stat == StatId.AttackThreshold)
                {
                    int vi = Mathf.RoundToInt(v);
                    vi = e.ModifyAttackThreshold(r, ctx, vi, ref applied);
                    v = vi;
                    if (applied) continue; // ← özel çalıştıysa genel çalışmasın
                }
                else if (stat == StatId.DefenseThreshold)
                {
                    int vi = Mathf.RoundToInt(v);
                    vi = e.ModifyDefenseThreshold(r, ctx, vi, ref applied);
                    v = vi;
                    if (applied) continue;
                }

                v = e.ModifyStat(r, ctx, stat, v, ref applied);
            }
        }
        return v;
    }



    public int ApplyAttackThresholdModifiers(int baseValue, Actor actor, PhaseKind phase)
        => Mathf.RoundToInt(ApplyStatModifiers(StatId.AttackThreshold, baseValue, actor, phase));

    public int ApplyDefenseThresholdModifiers(int baseValue, Actor actor, PhaseKind phase)
        => Mathf.RoundToInt(ApplyStatModifiers(StatId.DefenseThreshold, baseValue, actor, phase));
}
