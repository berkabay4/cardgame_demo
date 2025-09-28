using UnityEngine;
using System.Collections;
using System;

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
        var enemies = dir.GetComponent<GameDirector>() != null
            ? dir.GetComponent<GameDirector>() // no-op
            : null;

        var registry = (EnemyRegistry)dir.GetType()
            .GetField("_enemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(dir);

        // ENEMY DEF
        _state.SetStep(TurnStep.EnemyDef, dir.onStepChanged, dir.onLog);
        for (int i = 0; i < registry.All.Count; i++)
        {
            var e = registry.All[i];
            if (!e) continue;
            _ctx.SetEnemy(e);
            _onTurnIdx?.Invoke(e, i);
            _onPhaseStarted?.Invoke(e, PhaseKind.Defense);

            yield return _host.Run(RunPhaseWithDelays(PhaseKind.Defense));
            int totalDef = _ctx.GetAcc(Actor.Enemy, PhaseKind.Defense).Total;
            _state.EnemyDefTotals[e] = totalDef;
            _onPhaseEnded?.Invoke(e, PhaseKind.Defense, totalDef);
        }

        // ENEMY ATK
        _state.SetStep(TurnStep.EnemyAtk, dir.onStepChanged, dir.onLog);
        for (int i = 0; i < registry.All.Count; i++)
        {
            var e = registry.All[i];
            if (!e) continue;
            _ctx.SetEnemy(e);
            _onTurnIdx?.Invoke(e, i);
            _onPhaseStarted?.Invoke(e, PhaseKind.Attack);

            yield return _host.Run(RunPhaseWithDelays(PhaseKind.Attack));
            int atk = _ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;
            _state.EnemyAtkTotals[e] = atk;
            e.CurrentAttack = atk;
            _onPhaseEnded?.Invoke(e, PhaseKind.Attack, atk);
        }

        _state.IsBusy = false;
        onDone?.Invoke();
    }

    IEnumerator RunPhaseWithDelays(PhaseKind phase)
    {
        var enumerator = EnemyPolicy.BuildPhaseEnumerator(_ctx, phase);
        while (enumerator.MoveNext())
        {
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
