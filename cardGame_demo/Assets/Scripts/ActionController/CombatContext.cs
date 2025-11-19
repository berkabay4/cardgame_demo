// CombatContext.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CombatContext
{
    // ==== Global fallback rule ====
    public int Threshold;

    // ==== Per-phase thresholds (optional overrides) ====
    // (Actor, Phase) -> threshold
    private readonly Dictionary<(Actor, PhaseKind), int> _phaseThresholds = new();

    // ==== Units & Phases ====
    private readonly Dictionary<Actor, SimpleCombatant> _units = new(); // Actor -> Unit
    private readonly Dictionary<(Actor, PhaseKind), PhaseAccumulator> _phases = new(); // (Actor, Phase) -> Acc

    // Expose read-only for existing callers (e.g. StartTurnAction)
    public IReadOnlyDictionary<(Actor, PhaseKind), PhaseAccumulator> Phases => _phases;

    // ==== Decks ====
    // Unit -> Deck (her unit'in kendine ait runtime destesi)
    private readonly Dictionary<SimpleCombatant, IDeckService> _decksByUnit = new();

    // ==== Events ====
    public readonly UnityEvent<Actor, PhaseKind, int, int> OnProgress = new();
    public readonly UnityEvent<Actor, PhaseKind, Card> OnCardDrawn = new();
    public readonly UnityEvent<string> OnLog = new();

    // ==== Convenience ====
    public SimpleCombatant Player => TryGetUnit(Actor.Player, out var p) ? p : null;
    public SimpleCombatant Enemy  => TryGetUnit(Actor.Enemy,  out var e) ? e : null;

    public IDeckService PlayerDeck => GetDeckFor(Actor.Player);
    public IDeckService EnemyDeck  => GetDeckFor(Actor.Enemy);

    // ---- ctor ----
    public CombatContext(int threshold, IDeckService _unusedLegacyDeck = null,
                         SimpleCombatant player = null, SimpleCombatant enemy = null)
    {
        Threshold = threshold;

        if (player) RegisterUnit(Actor.Player, player);
        if (enemy)  RegisterUnit(Actor.Enemy,  enemy);

        // Varsayılan accumulator’ları aç (mevcutsa overwrite etmez)
        EnsurePhase(Actor.Player, PhaseKind.Defense, isPlayer: true);
        EnsurePhase(Actor.Player, PhaseKind.Attack,  isPlayer: true);
        EnsurePhase(Actor.Enemy,  PhaseKind.Defense, isPlayer: false);
        EnsurePhase(Actor.Enemy,  PhaseKind.Attack,  isPlayer: false);
    }

    // ======================================================
    // ================= Rules / Thresholds =================
    // ======================================================
    public int GetThreshold(Actor actor, PhaseKind phase)
    {
        return _phaseThresholds.TryGetValue((actor, phase), out var t) ? t : Threshold;
    }

    public void SetPhaseThreshold(Actor actor, PhaseKind phase, int value)
    {
        _phaseThresholds[(actor, phase)] = Mathf.Max(5, value);
        // UI’yı mevcut toplam ile yeni eşik üzerinden güncelle
        var acc = TryGetAcc(actor, phase, createIfMissing: false, isPlayerFlagIfCreate: actor == Actor.Player);
        if (acc != null) OnProgress?.Invoke(actor, phase, acc.Total, GetThreshold(actor, phase));
        OnLog?.Invoke($"[Rule] Threshold({actor},{phase}) → {GetThreshold(actor, phase)}");
    }

    /// <summary>Global Threshold’ı değiştirir (faz-spec override’ları korur).</summary>
    public void SetGlobalThreshold(int value, bool refreshProgress = true)
    {
        Threshold = Mathf.Max(5, value);
        OnLog?.Invoke($"[Rule] Global Threshold → {Threshold}");
        if (refreshProgress)
        {
            foreach (var kv in _phases)
            {
                var (actor, phase) = kv.Key;
                var acc = kv.Value;
                OnProgress?.Invoke(actor, phase, acc.Total, GetThreshold(actor, phase));
            }
        }
    }

    /// <summary>İsteğe bağlı: tüm faz özel eşiklerini temizler.</summary>
    public void ClearPhaseThresholdOverrides(bool refreshProgress = true)
    {
        _phaseThresholds.Clear();
        OnLog?.Invoke("[Rule] Phase threshold overrides cleared.");
        if (refreshProgress)
        {
            foreach (var kv in _phases)
            {
                var (actor, phase) = kv.Key;
                var acc = kv.Value;
                OnProgress?.Invoke(actor, phase, acc.Total, GetThreshold(actor, phase));
            }
        }
    }

    // ======================================================
    // ================ Units / Actors API ==================
    // ======================================================
    public void RegisterUnit(Actor actor, SimpleCombatant unit, IDeckService deck = null, bool overwrite = true)
    {
        if (!unit)
        {
            OnLog?.Invoke($"[Ctx] RegisterUnit ignored (null) for {actor}");
            return;
        }

        if (!overwrite && _units.ContainsKey(actor))
        {
            OnLog?.Invoke($"[Ctx] RegisterUnit skipped (exists) for {actor} -> {_units[actor].name}");
            return;
        }

        _units[actor] = unit;
        OnLog?.Invoke($"[Ctx] Unit registered: {actor} -> {unit.name}");

        if (deck != null)
            RegisterDeck(unit, deck);

        // Accumulators yoksa oluştur
        EnsurePhase(actor, PhaseKind.Defense, isPlayer: actor == Actor.Player);
        EnsurePhase(actor, PhaseKind.Attack,  isPlayer: actor == Actor.Player);
    }

    public bool TryGetUnit(Actor actor, out SimpleCombatant unit) => _units.TryGetValue(actor, out unit);
    public SimpleCombatant GetUnit(Actor actor) => _units.TryGetValue(actor, out var u) ? u : null;

    /// <summary>
    /// Düşmanı değiştir. Accumulator'ları sıfırla/koru ve deck'i taşı/koru seçenekleri.
    /// </summary>
    public void SetEnemy(SimpleCombatant newEnemy,
                         bool resetEnemyAccumulators = true,
                         bool carryOverExistingEnemyDeck = true)
    {
        if (!newEnemy) return;

        // Eski enemy’in deck’ini devret
        if (_units.TryGetValue(Actor.Enemy, out var oldEnemy) && oldEnemy && carryOverExistingEnemyDeck)
        {
            if (_decksByUnit.TryGetValue(oldEnemy, out var oldDeck) && oldDeck != null)
            {
                _decksByUnit[newEnemy] = oldDeck;
                _decksByUnit.Remove(oldEnemy);
                OnLog?.Invoke($"[Ctx] Enemy deck moved: {oldEnemy.name} -> {newEnemy.name}");
            }
        }

        _units[Actor.Enemy] = newEnemy;

        if (resetEnemyAccumulators)
        {
            _phases[(Actor.Enemy, PhaseKind.Defense)] = new PhaseAccumulator("E.DEF", isPlayer: false);
            _phases[(Actor.Enemy, PhaseKind.Attack)]  = new PhaseAccumulator("E.ATK",  isPlayer: false);

            OnProgress?.Invoke(Actor.Enemy, PhaseKind.Defense, 0, GetThreshold(Actor.Enemy, PhaseKind.Defense));
            OnProgress?.Invoke(Actor.Enemy, PhaseKind.Attack,  0, GetThreshold(Actor.Enemy, PhaseKind.Attack));

            OnLog?.Invoke($"[Ctx] Enemy accumulators reset for {newEnemy.name}");
        }

        OnLog?.Invoke($"[Ctx] Enemy set to {newEnemy.name}");
    }

    // ======================================================
    // ================== Phases / Acc API ==================
    // ======================================================
    public PhaseAccumulator GetAcc(Actor actor, PhaseKind phase) => _phases[(actor, phase)];

    public PhaseAccumulator TryGetAcc(Actor actor, PhaseKind phase, bool createIfMissing, bool isPlayerFlagIfCreate)
    {
        if (_phases.TryGetValue((actor, phase), out var acc) && acc != null)
            return acc;

        if (!createIfMissing) return null;

        acc = new PhaseAccumulator($"{(actor == Actor.Player ? "P" : "E")}.{phase}", isPlayerFlagIfCreate);
        _phases[(actor, phase)] = acc;
        return acc;
    }

    private void EnsurePhase(Actor actor, PhaseKind phase, bool isPlayer)
    {
        if (!_phases.ContainsKey((actor, phase)) || _phases[(actor, phase)] == null)
            _phases[(actor, phase)] = new PhaseAccumulator($"{(actor == Actor.Player ? "P" : "E")}.{phase}", isPlayer);
    }

    public void ResetPhases(Actor actor, bool log = true)
    {
        bool isPlayer = actor == Actor.Player;
        _phases[(actor, PhaseKind.Defense)] = new PhaseAccumulator($"{(isPlayer ? "P" : "E")}.DEF", isPlayer);
        _phases[(actor, PhaseKind.Attack)]  = new PhaseAccumulator($"{(isPlayer ? "P" : "E")}.ATK",  isPlayer);

        if (log)
        {
            OnProgress?.Invoke(actor, PhaseKind.Defense, 0, GetThreshold(actor, PhaseKind.Defense));
            OnProgress?.Invoke(actor, PhaseKind.Attack,  0, GetThreshold(actor, PhaseKind.Attack));
            OnLog?.Invoke($"[Ctx] Phases reset for {actor}");
        }
    }

    // ======================================================
    // ==================== Decks API =======================
    // ======================================================
    public void RegisterDeck(SimpleCombatant unit, IDeckService deck)
    {
        if (!unit || deck == null) return;
        _decksByUnit[unit] = deck;
        OnLog?.Invoke($"[Ctx] Deck registered for unit: {unit.name}");
    }

    public void SetDeckFor(Actor actor, IDeckService deck)
    {
        if (!TryGetUnit(actor, out var unit) || unit == null || deck == null) return;
        _decksByUnit[unit] = deck;
        OnLog?.Invoke($"[Ctx] Deck set for {actor} -> {unit.name}");
    }

    public IDeckService GetDeckFor(Actor actor)
    {
        if (!TryGetUnit(actor, out var unit) || unit == null) return null;
        return _decksByUnit.TryGetValue(unit, out var deck) ? deck : null;
    }

    public IDeckService GetDeckFor(SimpleCombatant unit)
    {
        if (!unit) return null;
        return _decksByUnit.TryGetValue(unit, out var deck) ? deck : null;
    }

    public bool TryGetDeckFor(Actor actor, out IDeckService deck)
    {
        deck = null;
        if (!TryGetUnit(actor, out var unit) || unit == null) return false;
        return _decksByUnit.TryGetValue(unit, out deck) && deck != null;
    }
    // ======================================================
    // ================== Damage Helpers ====================
    // ======================================================
    /// <summary>
    /// MiniBoss gibi sistemlerden Player'a direkt damage vurmak için sugar helper.
    /// Buradaki 'TakeDamage' kısmını kendi SimpleCombatant API'ne göre düzelt.
    /// </summary>
    public void DealDamageToPlayer(int amount)
    {
        var player = Player;
        if (player == null)
        {
            OnLog?.Invoke("[Ctx] DealDamageToPlayer called but Player unit is null.");
            return;
        }

        // TODO: SimpleCombatant'ta hangi metod varsa onu kullan:
        // Örnek: player.TakeDamage(amount);
        // veya    player.ApplyDamage(amount);
        // veya    player.Health.Change(-amount);

        OnLog?.Invoke($"[Ctx] (stub) DealDamageToPlayer({amount}) called for {player.name}, implement damage logic here.");
    }
    public IEnumerable<IDeckService> AllDecks()
    {
        foreach (var d in _decksByUnit.Values)
            if (d != null) yield return d;
    }

    public void UnregisterDeck(SimpleCombatant unit)
    {
        if (!unit) return;
        if (_decksByUnit.Remove(unit))
            OnLog?.Invoke($"[Ctx] Deck unregistered for unit: {unit.name}");
    }

    public void ClearAllDecks()
    {
        _decksByUnit.Clear();
        OnLog?.Invoke($"[Ctx] All decks cleared");
    }

    // ======================================================
    // ================== Helpers (sugar) ===================
    // ======================================================
    public void RegisterPlayer(SimpleCombatant player, IDeckService deck = null)
        => RegisterUnit(Actor.Player, player, deck, overwrite: true);

    public void RegisterEnemy(SimpleCombatant enemy, IDeckService deck = null)
        => RegisterUnit(Actor.Enemy, enemy, deck, overwrite: true);
}
