// CombatContext.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CombatContext
{
    public int Threshold;

    // ESKİ: public readonly IDeckService Deck;
    // YENİ:
    // Her bir unit için ayrı deste
    public readonly Dictionary<SimpleCombatant, IDeckService> DecksByUnit = new();

    public readonly Dictionary<(Actor,PhaseKind), PhaseAccumulator> Phases = new();
    public readonly Dictionary<Actor, SimpleCombatant> Units = new();

    public readonly UnityEvent<Actor,PhaseKind,int,int> OnProgress = new();
    public readonly UnityEvent<Actor,PhaseKind,Card> OnCardDrawn = new();
    public readonly UnityEvent<string> OnLog = new();

    // Convenience
    public SimpleCombatant Player => Units.TryGetValue(Actor.Player, out var p) ? p : null;
    public SimpleCombatant Enemy  => Units.TryGetValue(Actor.Enemy,  out var e) ? e : null;

    public CombatContext(int threshold, IDeckService _unusedLegacyDeck, SimpleCombatant player, SimpleCombatant enemy)
    {
        Threshold = threshold;
        Units[Actor.Player] = player;
        Units[Actor.Enemy]  = enemy;

        Phases[(Actor.Player, PhaseKind.Defense)] = new PhaseAccumulator("P.DEF", isPlayer: true);
        Phases[(Actor.Player, PhaseKind.Attack)]  = new PhaseAccumulator("P.ATK", isPlayer: true);
        Phases[(Actor.Enemy,  PhaseKind.Defense)] = new PhaseAccumulator("E.DEF", isPlayer: false);
        Phases[(Actor.Enemy,  PhaseKind.Attack)]  = new PhaseAccumulator("E.ATK",  isPlayer: false);
    }

    public PhaseAccumulator GetAcc(Actor a, PhaseKind k) => Phases[(a,k)];
    public SimpleCombatant GetUnit(Actor a) => Units[a];

    // === Yeni: Deck API ===
    public void RegisterDeck(SimpleCombatant unit, IDeckService deck)
    {
        if (unit == null || deck == null) return;
        DecksByUnit[unit] = deck;
        OnLog?.Invoke($"[Ctx] Deck registered: {unit.name}");
    }

    public IDeckService GetDeckFor(Actor a)
    {
        if (!Units.TryGetValue(a, out var unit) || unit == null) return null;
        return DecksByUnit.TryGetValue(unit, out var deck) ? deck : null;
    }

    public IEnumerable<IDeckService> AllDecks()
    {
        return DecksByUnit.Values;
    }

    // Düşman swap — deck aynı kalır, sadece phase accumulator resetlenir
    public void SetEnemy(SimpleCombatant enemy, bool resetEnemyAccumulators = true)
    {
        if (enemy == null) return;

        Units[Actor.Enemy] = enemy;

        if (resetEnemyAccumulators)
        {
            Phases[(Actor.Enemy, PhaseKind.Defense)] = new PhaseAccumulator("E.DEF", isPlayer: false);
            Phases[(Actor.Enemy, PhaseKind.Attack)]  = new PhaseAccumulator("E.ATK",  isPlayer: false);

            OnProgress?.Invoke(Actor.Enemy, PhaseKind.Defense, 0, Threshold);
            OnProgress?.Invoke(Actor.Enemy, PhaseKind.Attack,  0, Threshold);
        }

        OnLog?.Invoke($"[Ctx] Enemy set to {enemy.name}");
    }
}
