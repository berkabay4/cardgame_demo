using System.Collections.Generic;
using UnityEngine;

public class BattleState
{
    public TurnStep Step { get; private set; }

    public bool IsBusy;
    public bool WaitingForTarget;
    public SimpleCombatant CurrentTarget;

    public int PlayerDefTotal;
    public int PlayerAtkTotal;

    public readonly Dictionary<SimpleCombatant,int> EnemyDefTotals = new();
    public readonly Dictionary<SimpleCombatant,int> EnemyAtkTotals = new();

    public void ResetForNewTurn()
    {
        WaitingForTarget = false;
        CurrentTarget = null;
        PlayerDefTotal = 0;
        PlayerAtkTotal = 0;
        EnemyDefTotals.Clear();
        EnemyAtkTotals.Clear();
        IsBusy = false;
    }

    public void SetStep(TurnStep s, StepEvent onStepChanged, UnityEngine.Events.UnityEvent<string> onLog)
    {
        Step = s;
        onStepChanged?.Invoke(Step);
        onLog?.Invoke(s switch
        {
            TurnStep.PlayerDef    => "Your Defense: Draw or Accept.",
            TurnStep.PlayerAtk    => "Your Attack: Draw or Accept.",
            TurnStep.SelectTarget => "Select a target for your locked Attack.",
            TurnStep.EnemyDef     => "Enemies choosing Defense (in order)...",
            TurnStep.EnemyAtk     => "Enemies choosing Attack (in order)...",
            TurnStep.Resolve      => "Resolving...",
            _ => ""
        });
    }
}
