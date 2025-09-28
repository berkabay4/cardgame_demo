using UnityEngine;

public class TargetingController
{
    readonly GameDirector _dir;
    readonly BattleState _state;
    readonly EnemyRegistry _enemies;
    readonly BoolEvent _onWaitingChanged;
    readonly TargetEvent _onTargetChanged;
    readonly UnityEngine.Events.UnityEvent<string> _log;

    public TargetingController(GameDirector dir, BattleState state, EnemyRegistry enemies,
                               BoolEvent onWaitingChanged, TargetEvent onTargetChanged,
                               UnityEngine.Events.UnityEvent<string> log)
    {
        _dir = dir; _state = state; _enemies = enemies;
        _onWaitingChanged = onWaitingChanged; _onTargetChanged = onTargetChanged; _log = log;
    }

    public void BeginTargetMode(int atkTotal, bool TryAutoTargetSingle)
    {
        SetWaiting(true);
        _log?.Invoke($"[SelectTarget] Your ATK={atkTotal}. Click an enemy to target.");
        if (TryAutoTargetSingle) TryAutoSelectSingleEnemy();
    }

    public void CancelTargetMode() => SetWaiting(false);

    public bool TrySelectTarget(SimpleCombatant enemy)
    {
        if (_state.Step != TurnStep.SelectTarget || !_state.WaitingForTarget)
        {
            _log?.Invoke("[SelectTarget] Not expecting a target now.");
            return false;
        }
        if (enemy == null || !_enemies.All.Contains(enemy))
        {
            _log?.Invoke("[SelectTarget] Invalid enemy.");
            return false;
        }

        _state.CurrentTarget = enemy;
        _onTargetChanged?.Invoke(enemy);
        SetWaiting(false);
        return true;
    }

    void TryAutoSelectSingleEnemy()
    {
        var alive = _enemies.AliveEnemies;
        if (alive.Count == 1 && _state.WaitingForTarget && _state.Step == TurnStep.SelectTarget)
        {
            _state.CurrentTarget = alive[0];
            _onTargetChanged?.Invoke(_state.CurrentTarget);
            SetWaiting(false);
            _log?.Invoke("[SelectTarget] Single enemy detected. Auto-targeted.");
        }
    }

    void SetWaiting(bool v)
    {
        if (_state.WaitingForTarget == v) return;
        _state.WaitingForTarget = v;
        _log?.Invoke($"[State] waitingForTarget = {v} (step={_state.Step})");
        _onWaitingChanged?.Invoke(v);
    }
}
