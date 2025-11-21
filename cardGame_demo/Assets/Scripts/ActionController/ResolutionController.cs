using UnityEngine;
using System.Collections;
using System.Linq;

public interface ICoroutineHost
{
    Coroutine Run(IEnumerator routine);
}

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

    public ResolutionController(
        ICoroutineHost host,
        CombatContext ctx,
        BattleState state,
        EnemyRegistry enemies,
        float enemyAttackSpacing,
        UnityEngine.Events.UnityEvent<string> log,
        UnityEngine.Events.UnityEvent onRoundResolved,
        UnityEngine.Events.UnityEvent onGameOver,
        UnityEngine.Events.UnityEvent onGameWin,
        IAnimationBridge anim)
    {
        _host               = host;
        _ctx                = ctx;
        _state              = state;
        _enemies            = enemies;
        _enemyAttackSpacing = enemyAttackSpacing;
        _log                = log;
        _onRoundResolved    = onRoundResolved;
        _onGameOver         = onGameOver;
        _onGameWin          = onGameWin;
        _anim               = anim;
    }

    public IEnumerator ResolveRoundAndRestart()
    {
        var director = CombatDirector.Instance;
        if (director != null)
        {
            director.State.SetStep(TurnStep.Resolve, director.onStepChanged, _log);
        }

        // =====================================================
        // 1) PLAYER → TARGET
        // =====================================================
        int playerAtkTotal = Mathf.Max(0, _state.PlayerAtkTotal);

        if (_state.CurrentTarget != null &&
            _state.CurrentTarget.CurrentHP > 0 &&
            playerAtkTotal > 0)                     // <-- Attack 0 ise hiç animasyon yok
        {
            int targetDef = _state.EnemyDefTotals.TryGetValue(_state.CurrentTarget, out var d)
                ? Mathf.Max(0, d)
                : 0;

            int dmg = Mathf.Max(0, playerAtkTotal - targetDef);

            yield return _host.Run(
                _anim.PlayAttackAnimation(_ctx.Player, _state.CurrentTarget, dmg, () =>
                {
                    if (dmg > 0)
                    {
                        _state.CurrentTarget.TakeDamage(dmg);
                        _log?.Invoke($"You dealt {dmg} to {_state.CurrentTarget.name}.");
                    }
                    else
                    {
                        _log?.Invoke($"Your attack couldn’t pierce {_state.CurrentTarget.name}'s defense.");
                    }
                })
            );
        }
        else
        {
            if (playerAtkTotal <= 0)
                _log?.Invoke("You have no attack this round.");
            else
                _log?.Invoke("No valid target for your attack.");
        }

        // =====================================================
        // 2) ENEMIES → PLAYER (sırayla)
        //    MiniBoss / Boss pattern burada devreye giriyor.
        // =====================================================
        int remainingDef = Mathf.Max(0, _state.PlayerDefTotal);

        var alive = _enemies.All
            .Where(e => e && e.CurrentHP > 0)
            .ToList();

        int lastActiveIdx = -1;
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i])
            {
                lastActiveIdx = i;
                break;
            }
        }

        for (int i = 0; i < alive.Count; i++)
        {
            var enemy = alive[i];
            if (!enemy) continue;

            int enemyAtk = _state.EnemyAtkTotals.TryGetValue(enemy, out var atk)
                ? Mathf.Max(0, atk)
                : 0;

            // Attack 0 ise hiç saldırı / animasyon yok
            if (enemyAtk > 0)
            {
                // --- Player DEF uygula (toplam stand’lar üzerinden) ---
                int effective = Mathf.Max(0, enemyAtk - remainingDef);
                int blocked   = Mathf.Min(remainingDef, enemyAtk);
                remainingDef  = Mathf.Max(0, remainingDef - enemyAtk);

                // MiniBoss / Boss mu?
                var mini = enemy.GetComponent<MiniBossRuntime>();
                var director2 = CombatDirector.Instance;
                bool isMiniBoss = mini != null &&
                                  mini.Definition != null &&
                                  mini.Definition.attackBehaviour != null &&
                                  director2 != null;

                if (isMiniBoss)
                {
                    // === MINI BOSS / BOSS BRANCH ===
                    int roundIndex = director2.NextEnemyAttackRound();

                    var info = new EnemyAttackContextInfo
                    {
                        fightKind        = (EnemyFightKind)director2.CurrentFightKind,
                        turnIndex        = director2.TurnIndex,
                        attackRoundIndex = roundIndex
                    };

                    _log?.Invoke(
                        $"[MiniBoss] Attack phase → baseATK={enemyAtk}, " +
                        $"effective={effective}, blocked={blocked}, " +
                        $"turn={info.turnIndex}, round={info.attackRoundIndex}"
                    );

                    // Burada baseAttackValue = enemyAtk (DEF sonrası değil),
                    // behaviour içinden kaç vuruş / nasıl vuracağına karar veriyor.
                    yield return _host.Run(
                        mini.Definition.attackBehaviour.ExecuteAttackCoroutine(
                            _anim,
                            _ctx,
                            enemy,
                            enemyAtk,
                            info
                        )
                    );
                }
                else
                {
                    // === NORMAL ENEMY BRANCH ===
                    int damageToApply = effective; // 0 olabilir, ama animasyon yine oynar

                    yield return _host.Run(
                        _anim.PlayAttackAnimation(enemy, _ctx.Player, damageToApply, () =>
                        {
                            if (damageToApply > 0)
                            {
                                _ctx.Player.TakeDamage(damageToApply);
                                _log?.Invoke($"{enemy.name} hits you for {damageToApply}.");
                            }
                            else
                            {
                                _log?.Invoke($"{enemy.name}'s attack was blocked.");
                            }
                        })
                    );
                }
            }
            else
            {
                _log?.Invoke($"{enemy.name} has no attack this round.");
            }

            // birden fazla düşman varsa aralarına spacing koy
            if (i < lastActiveIdx && _enemyAttackSpacing > 0f)
                yield return new WaitForSeconds(_enemyAttackSpacing);
        }

        // =====================================================
        // 3) Win / Lose kontrolü
        // =====================================================
        if (CheckWinLose())
            yield break;

        // =====================================================
        // 4) Cleanup & next turn
        // =====================================================
        _ctx.Player.CurrentAttack = 0;
        foreach (var e in _enemies.All)
            if (e) e.CurrentAttack = 0;

        _onRoundResolved?.Invoke();

        RelicManager.Instance?.OnTurnEnd();

        CombatDirector.Instance.StartNewTurn();
    }

    bool CheckWinLose()
    {
        var aliveEnemies = _enemies.AliveEnemies;

        if (_ctx.Player.CurrentHP <= 0 && aliveEnemies.Count > 0)
        {
            _log?.Invoke("Game Over");
            _onGameOver?.Invoke();
            CombatDirector.Instance.ResetCombatState();
            return true;
        }

        if (aliveEnemies.Count == 0 && _ctx.Player.CurrentHP > 0)
        {
            _log?.Invoke("Game Win!");
            _onGameWin?.Invoke();
            CombatDirector.Instance.ResetCombatState();
            return true;
        }

        if (_ctx.Player.CurrentHP <= 0 && aliveEnemies.Count == 0)
        {
            _log?.Invoke("Draw! (both defeated)");
            CombatDirector.Instance.ResetCombatState();
            return true;
        }

        return false;
    }
}
