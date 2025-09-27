// CombatContext.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CombatContext
{
    public int Threshold;
    public readonly IDeckService Deck;
    public readonly Dictionary<(Actor,PhaseKind), PhaseAccumulator> Phases = new();
    public readonly Dictionary<Actor, SimpleCombatant> Units = new();

    // UnityEvent köprüsü (UI için)
    public readonly UnityEvent<Actor,PhaseKind,int,int> OnProgress = new();
    public readonly UnityEvent<Actor,PhaseKind,Card> OnCardDrawn = new();
    public readonly UnityEvent<string> OnLog = new();

    public CombatContext(int threshold, IDeckService deck, SimpleCombatant player, SimpleCombatant enemy)
    {
        Threshold = threshold; Deck = deck;
        Units[Actor.Player] = player; Units[Actor.Enemy] = enemy;
        Phases[(Actor.Player, PhaseKind.Defense)] = new PhaseAccumulator("P.DEF");
        Phases[(Actor.Player, PhaseKind.Attack)]  = new PhaseAccumulator("P.ATK");
        Phases[(Actor.Enemy,  PhaseKind.Defense)] = new PhaseAccumulator("E.DEF");
        Phases[(Actor.Enemy,  PhaseKind.Attack)]  = new PhaseAccumulator("E.ATK");
    }

    public PhaseAccumulator GetAcc(Actor a, PhaseKind k) => Phases[(a,k)];
    public SimpleCombatant GetUnit(Actor a) => Units[a];
}
