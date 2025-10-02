public class TargetingController
{
    readonly CombatDirector _host;
    readonly BattleState _state;
    readonly EnemyRegistry _enemies;
    readonly UnityEngine.Events.UnityEvent<bool> _onWaitingChanged;
    readonly UnityEngine.Events.UnityEvent<SimpleCombatant> _onTargetChanged;
    readonly UnityEngine.Events.UnityEvent<string> _log;

    public TargetingController(CombatDirector host, BattleState state, EnemyRegistry enemies,
                               UnityEngine.Events.UnityEvent<bool> onWaitingChanged,
                               UnityEngine.Events.UnityEvent<SimpleCombatant> onTargetChanged,
                               UnityEngine.Events.UnityEvent<string> log)
    {
        _host = host; _state = state; _enemies = enemies;
        _onWaitingChanged = onWaitingChanged; _onTargetChanged = onTargetChanged; _log = log;
    }

    public void BeginTargetMode(int playerAtkTotal, bool TryAutoTargetSingle)
    {
        var alive = _enemies.AliveEnemies;

        // Tek düşman varsa istersen otomatik seç
        if (TryAutoTargetSingle && alive.Count == 1)
        {
            _state.CurrentTarget = alive[0];
            _state.WaitingForTarget = false;
            _onWaitingChanged?.Invoke(false);
            _onTargetChanged?.Invoke(_state.CurrentTarget);
            _log?.Invoke($"[Target] Auto: {_state.CurrentTarget.name}");
            return;
        }

        // Aksi halde bekleme modunu AÇ
        _state.CurrentTarget = null;
        _state.WaitingForTarget = true;
        _onWaitingChanged?.Invoke(true);
        _log?.Invoke("[Target] Choose an enemy.");
    }

    public void CancelTargetMode()
    {
        _state.CurrentTarget = null;
        _state.WaitingForTarget = false;
        _onWaitingChanged?.Invoke(false);
        _log?.Invoke("[Target] Cancel");
    }

    public bool TrySelectTarget(SimpleCombatant enemy)
    {
        if (!_state.WaitingForTarget || enemy == null) return false;
        if (!_enemies.AliveEnemies.Contains(enemy)) return false;

        _state.CurrentTarget = enemy;
        _state.WaitingForTarget = false;
        _onWaitingChanged?.Invoke(false);
        _onTargetChanged?.Invoke(enemy);
        _log?.Invoke($"[Target] Selected: {enemy.name}");
        return true;
    }
}
