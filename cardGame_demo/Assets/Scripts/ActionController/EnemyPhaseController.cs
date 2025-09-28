using UnityEngine;
using System;
using System.Collections;

public class EnemyPhaseController
{
    readonly ICoroutineHost _host;
    readonly CombatContext _ctx;
    readonly ActionQueue _queue;
    readonly BattleState _state;

    readonly EnemyIdxEvent _onTurnIdx;
    readonly EnemyPhaseStartedEvent _onPhaseStarted;
    readonly EnemyPhaseEndedEvent _onPhaseEnded;
    readonly Vector2 _drawDelayRange;
    readonly UnityEngine.Events.UnityEvent<string> _log;

    public Coroutine Running;

    public EnemyPhaseController(ICoroutineHost host, CombatContext ctx, ActionQueue queue, BattleState state,
                                EnemyIdxEvent onTurnIdx, EnemyPhaseStartedEvent onPhaseStarted, EnemyPhaseEndedEvent onPhaseEnded,
                                Vector2 drawDelayRange, UnityEngine.Events.UnityEvent<string> log)
    {
        _host = host; _ctx = ctx; _queue = queue; _state = state;
        _onTurnIdx = onTurnIdx; _onPhaseStarted = onPhaseStarted; _onPhaseEnded = onPhaseEnded;
        _drawDelayRange = drawDelayRange; _log = log;
    }

    public IEnumerator PrecomputeBothPhasesThen(Action onDone)
    {
        var dir = GameDirector.Instance;
        if (!dir) yield break;

        // EnemyRegistry'yi (şimdilik) refleksiyonla al
        var registryField = typeof(GameDirector).GetField("_enemies",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var registry = (EnemyRegistry)registryField?.GetValue(dir);
        if (registry == null || registry.All == null) yield break;

        // === ENEMY DEFENSE ===
        _state.SetStep(TurnStep.EnemyDef, dir.onStepChanged, dir.onLog);
        for (int i = 0; i < registry.All.Count; i++)
        {
            var e = registry.All[i];
            if (!e) continue;

            // Bu düşman için faz-eşiklerini uygula
            ApplyEnemyPhaseThresholdsFor(e);

            // Aktif düşmanı bağla + accumulator resetle
            _ctx.SetEnemy(e, resetEnemyAccumulators: true, carryOverExistingEnemyDeck: false);

            _onTurnIdx?.Invoke(e, i);
            _onPhaseStarted?.Invoke(e, PhaseKind.Defense);

            yield return _host.Run(RunPhaseWithDelays(PhaseKind.Defense));

            int totalDef = _ctx.GetAcc(Actor.Enemy, PhaseKind.Defense).Total;
            _state.EnemyDefTotals[e] = totalDef;
            _onPhaseEnded?.Invoke(e, PhaseKind.Defense, totalDef);
        }

        // === ENEMY ATTACK ===
        _state.SetStep(TurnStep.EnemyAtk, dir.onStepChanged, dir.onLog);
        for (int i = 0; i < registry.All.Count; i++)
        {
            var e = registry.All[i];
            if (!e) continue;

            // Bu düşman için faz-eşiklerini uygula
            ApplyEnemyPhaseThresholdsFor(e);

            // Aktif düşmanı bağla + accumulator resetle
            _ctx.SetEnemy(e, resetEnemyAccumulators: true, carryOverExistingEnemyDeck: false);

            _onTurnIdx?.Invoke(e, i);
            _onPhaseStarted?.Invoke(e, PhaseKind.Attack);

            yield return _host.Run(RunPhaseWithDelays(PhaseKind.Attack));

            int atk = _ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;
            _state.EnemyAtkTotals[e] = atk;
            e.CurrentAttack = atk;
            _onPhaseEnded?.Invoke(e, PhaseKind.Attack, atk);
        }

        onDone?.Invoke();
    }

    /// <summary>
    /// Verilen düşman için EnemyData’daki ATK/DEF maksimumlarını Ctx’e yazar.
    /// Data yoksa mevcut ctx eşikleri olduğu gibi kalır (fallback olarak global threshold).
    /// </summary>
    void ApplyEnemyPhaseThresholdsFor(SimpleCombatant enemy)
    {
        if (!enemy || _ctx == null) return;

        var prov = enemy.GetComponent<EnemyTargetRangeProvider>();
        var data = prov ? prov.enemyData : null;

        // data varsa onu kullan; yoksa mevcut ctx per-phase threshold veya global threshold korunur.
        if (data != null)
        {
            int atkMax = Mathf.Max(5, data.maxAttackRange);
            int defMax = Mathf.Max(5, data.maxdefenceRange);

            _ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Attack,  atkMax);
            _ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Defense, defMax);

            _ctx.OnLog?.Invoke($"[AI] Thresholds set for {enemy.name} → ATK:{atkMax}, DEF:{defMax}");
        }
    }

    IEnumerator RunPhaseWithDelays(PhaseKind phase)
    {
        var acc = _ctx.GetAcc(Actor.Enemy, phase);
        var enumerator = EnemyPolicy.BuildPhaseEnumerator(_ctx, phase);

        int safetySteps = 0;
        const int MAX_STEPS = 64; // sonsuz döngü koruması

        while (enumerator.MoveNext())
        {
            if (acc.IsStanding || acc.IsBusted)
            {
                _ctx.OnLog?.Invoke($"[AI] Enemy {phase} stopped (Standing={acc.IsStanding}, Busted={acc.IsBusted}).");
                break;
            }

            if (++safetySteps > MAX_STEPS)
            {
                _ctx.OnLog?.Invoke($"[AI] Enemy {phase} abort (safety {MAX_STEPS}). Forcing STAND.");
                _queue.Enqueue(new StandAction(Actor.Enemy, phase));
                yield return _host.Run(_queue.RunAllCoroutine(_ctx));
                break;
            }

            var action = enumerator.Current;
            _queue.Enqueue(action);
            yield return _host.Run(_queue.RunAllCoroutine(_ctx));

            if (action is DrawCardAction)
            {
                float delay = UnityEngine.Random.Range(_drawDelayRange.x, _drawDelayRange.y);
                yield return new WaitForSeconds(delay);
            }
        }

        _ctx.OnLog?.Invoke($"[AI] Enemy {phase} done: {_ctx.GetAcc(Actor.Enemy, phase).Total}");
    }
}
