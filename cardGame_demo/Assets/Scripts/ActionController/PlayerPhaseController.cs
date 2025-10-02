using UnityEngine;
using System.Collections;

public class PlayerPhaseController
{
    readonly ICoroutineHost _host;
    readonly CombatContext _ctx;
    readonly ActionQueue _queue;
    readonly BattleState _state;

    readonly IntEvent _onDefLocked;
    readonly IntEvent _onAtkLocked;
    readonly UnityEngine.Events.UnityEvent<string> _log;

    public PlayerPhaseController(ICoroutineHost host, CombatContext ctx, ActionQueue queue, BattleState state,
                                 IntEvent onDefLocked, IntEvent onAtkLocked,
                                 UnityEngine.Events.UnityEvent<string> log)
    {
        _host = host; _ctx = ctx; _queue = queue; _state = state;
        _onDefLocked = onDefLocked; _onAtkLocked = onAtkLocked; _log = log;
    }

    public void ResetAccumulator(PhaseKind kind)
    {
        var acc = _ctx.GetAcc(Actor.Player, kind);
        acc.Reset();
        _log?.Invoke(kind == PhaseKind.Defense ? "Your Defense: Draw or Accept." : "Your Attack: Draw or Accept.");
    }

    public IEnumerator DrawDefense()
    {
        yield return _host.Run(EnqueueAndRun(() => _queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Defense))));

        var acc = _ctx.GetAcc(Actor.Player, PhaseKind.Defense);
        if (acc.IsBusted)
        {
            _state.PlayerDefTotal = 0;
            CombatDirector.Instance.BeginPhase(TurnStep.PlayerAtk);
            yield break;
        }
        if (acc.IsStanding)
        {
            _state.PlayerDefTotal = acc.Total;
            _onDefLocked?.Invoke(_state.PlayerDefTotal);
            CombatDirector.Instance.BeginPhase(TurnStep.PlayerAtk);
        }
    }
    public IEnumerator DrawAttack()
    {
        yield return _host.Run(EnqueueAndRun(() => 
            _queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack))));

        var acc = _ctx.GetAcc(Actor.Player, PhaseKind.Attack);
        if (acc.IsBusted)
        {
            _state.PlayerAtkTotal = 0;

            // Eğer CombatContext'e Player property eklediysen:
            _ctx.Player.CurrentAttack = 0;
            // (Ekli değilse: _ctx.GetUnit(Actor.Player).CurrentAttack = 0;)

            CombatDirector.Instance.BeginPhase(TurnStep.Resolve);
            CombatDirector.Instance.ResolveNow();   // <— TEMİZ çağrı
            yield break;
        }

        if (acc.IsStanding)
        {
            _state.PlayerAtkTotal = acc.Total;

            // Eğer CombatContext'e Player property eklediysen:
            _ctx.Player.CurrentAttack = _state.PlayerAtkTotal;
            // (Ekli değilse: _ctx.GetUnit(Actor.Player).CurrentAttack = _state.PlayerAtkTotal;)

            _onAtkLocked?.Invoke(_state.PlayerAtkTotal);
            CombatDirector.Instance.BeginPhase(TurnStep.SelectTarget);
        }
    }
    public IEnumerator AcceptDefense(System.Action onNext)
    {
        yield return _host.Run(EnqueueAndRun(() => _queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Defense))));
        _state.PlayerDefTotal = _ctx.GetAcc(Actor.Player, PhaseKind.Defense).Total;
        _onDefLocked?.Invoke(_state.PlayerDefTotal);
        onNext?.Invoke();
    }

    public IEnumerator AcceptAttack(System.Action onNext)
    {
        yield return _host.Run(EnqueueAndRun(() => 
            _queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Attack))));

        _state.PlayerAtkTotal = _ctx.GetAcc(Actor.Player, PhaseKind.Attack).Total;

        // Player property varsa:
        _ctx.Player.CurrentAttack = _state.PlayerAtkTotal;
        // Yoksa:
        // _ctx.GetUnit(Actor.Player).CurrentAttack = _state.PlayerAtkTotal;

        _onAtkLocked?.Invoke(_state.PlayerAtkTotal);
        onNext?.Invoke();
    }

    IEnumerator EnqueueAndRun(System.Action enqueue)
    {
        _state.IsBusy = true;
        enqueue?.Invoke();
        yield return _host.Run(_queue.RunAllCoroutine(_ctx));
        _state.IsBusy = false;
    }
}
