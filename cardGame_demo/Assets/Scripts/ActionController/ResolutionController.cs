using UnityEngine;
using System.Collections;
using System.Linq;

public interface ICoroutineHost { Coroutine Run(System.Collections.IEnumerator routine); }
public interface IAnimationBridge
{
    IEnumerator PlayAttackAnimation(SimpleCombatant attacker, SimpleCombatant defender, int damage, System.Action onImpact);
}

public class ResolutionController
{
    readonly ICoroutineHost _host;
    readonly CombatContext _ctx;
    readonly BattleState _state;
    readonly EnemyRegistry _enemies;
    readonly float _enemyAttackSpacing;
    readonly UnityEngine.Events.UnityEvent<string> _log;
    readonly UnityEngine.Events.UnityEvent _onRoundResolved;
    readonly UnityEngine.Events.UnityEvent _onGameOver, _onGameWin;
    readonly IAnimationBridge _anim;

    public ResolutionController(ICoroutineHost host, CombatContext ctx, BattleState state, EnemyRegistry enemies,
                                float enemyAttackSpacing, UnityEngine.Events.UnityEvent<string> log,
                                UnityEngine.Events.UnityEvent onRoundResolved,
                                UnityEngine.Events.UnityEvent onGameOver, UnityEngine.Events.UnityEvent onGameWin,
                                IAnimationBridge anim)
    {
        _host = host; _ctx = ctx; _state = state; _enemies = enemies;
        _enemyAttackSpacing = enemyAttackSpacing; _log = log;
        _onRoundResolved = onRoundResolved; _onGameOver = onGameOver; _onGameWin = onGameWin; _anim = anim;
    }

    public IEnumerator ResolveRoundAndRestart()
    {
        GameDirector.Instance.State.SetStep(TurnStep.Resolve, GameDirector.Instance.onStepChanged, _log);

        // 1) Player -> Target
        if (_state.CurrentTarget != null && _state.CurrentTarget.CurrentHP > 0)
        {
            int targetDef = _state.EnemyDefTotals.TryGetValue(_state.CurrentTarget, out var d) ? Mathf.Max(0, d) : 0;
            int dmg = Mathf.Max(0, Mathf.Max(0, _state.PlayerAtkTotal) - targetDef);

            yield return _host.Run(_anim.PlayAttackAnimation(_ctx.Player, _state.CurrentTarget, dmg, () =>
            {
                if (dmg > 0)
                {
                    // HealthManager üzerinden uygula
                    _state.CurrentTarget.TakeDamage(dmg);
                    _log?.Invoke($"You dealt {dmg} to {_state.CurrentTarget.name}.");
                }
                else
                {
                    _log?.Invoke($"Your attack couldn’t pierce {_state.CurrentTarget.name}'s defense.");
                }
            }));
        }
        else
        {
            _log?.Invoke("No valid target for your attack.");
        }

        // 2) Enemies -> Player (sırayla)
        int remainingDef = Mathf.Max(0, _state.PlayerDefTotal);

        // son aktif düşman
        var alive = _enemies.All.Where(e => e && e.CurrentHP > 0).ToList();
        int lastActiveIdx = -1;
        for (int i = alive.Count - 1; i >= 0; i--) { if (alive[i]) { lastActiveIdx = i; break; } }

        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            int enemyAtk = _state.EnemyAtkTotals.TryGetValue(e, out var atk) ? Mathf.Max(0, atk) : 0;

            if (enemyAtk > 0)
            {
                int effective = Mathf.Max(0, enemyAtk - remainingDef);
                remainingDef = Mathf.Max(0, remainingDef - enemyAtk);

                yield return _host.Run(_anim.PlayAttackAnimation(e, _ctx.Player, effective, () =>
                {
                    if (effective > 0)
                    {
                        // HealthManager üzerinden uygula
                        _ctx.Player.TakeDamage(effective);
                        _log?.Invoke($"{e.name} hits you for {effective}.");
                    }
                    else
                    {
                        _log?.Invoke($"{e.name}'s attack was blocked.");
                    }
                }));
            }
            else
            {
                _log?.Invoke($"{e.name} attacks but has no effective attack.");
            }

            if (i < lastActiveIdx && _enemyAttackSpacing > 0f)
                yield return new WaitForSeconds(_enemyAttackSpacing);
        }

        // 3) Win/Lose
        if (CheckWinLose()) yield break;

        // 4) Cleanup & next turn
        _ctx.Player.CurrentAttack = 0;
        foreach (var e in _enemies.All) if (e) e.CurrentAttack = 0;
        _onRoundResolved?.Invoke();

        GameDirector.Instance.StartNewTurn();
    }

    bool CheckWinLose()
    {
        var aliveEnemies = _enemies.AliveEnemies;

        if (_ctx.Player.CurrentHP <= 0 && aliveEnemies.Count > 0) { _log?.Invoke("Game Over"); _onGameOver?.Invoke(); return true; }
        if (aliveEnemies.Count == 0 && _ctx.Player.CurrentHP > 0) { _log?.Invoke("Game Win!"); _onGameWin?.Invoke(); return true; }
        if (_ctx.Player.CurrentHP <= 0 && aliveEnemies.Count == 0) { _log?.Invoke("Draw! (both defeated)"); return true; }
        return false;
    }
}
