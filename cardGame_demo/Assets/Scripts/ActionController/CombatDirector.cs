using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public enum TurnStep { PlayerDef, PlayerAtk, SelectTarget, EnemyDef, EnemyAtk, Resolve }

// === State Event Types (aynen korunur) ===
[System.Serializable] public class StepEvent : UnityEvent<TurnStep> { }
[System.Serializable] public class BoolEvent : UnityEvent<bool> { }
[System.Serializable] public class EnemyIdxEvent : UnityEvent<SimpleCombatant, int> { }
[System.Serializable] public class EnemyPhaseStartedEvent : UnityEvent<SimpleCombatant, PhaseKind> { }
[System.Serializable] public class EnemyPhaseEndedEvent : UnityEvent<SimpleCombatant, PhaseKind, int> { }
[System.Serializable] public class IntEvent : UnityEvent<int> { }
[System.Serializable] public class TargetEvent : UnityEvent<SimpleCombatant> { }

public enum FightKind
{
    Minor,
    EliteMiniBoss,
    Boss
}

[DisallowMultipleComponent]
public class CombatDirector : MonoBehaviour, ICoroutineHost, IAnimationBridge
{
    public static event System.Action ContextReady;
    public static CombatDirector Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoStartOnAwake = false;
    public UnityEvent onGameStarted;
    static int? s_LastPlayerHP;

    // === Fight / pattern state ===

    /// <summary>Bu combat’taki dövüş tipi. CombatFlowAdapter’dan set et.</summary>
    public FightKind CurrentFightKind { get; private set; } = FightKind.Minor;

    /// <summary>Kaçıncı el (1,2,3...). Her StartNewTurn’de +1.</summary>
    public int TurnIndex { get; private set; }

    /// <summary>Bu elde kaçıncı enemy saldırısı (1,2,3...). Sadece Resolve sırasında artar.</summary>
    public int EnemyAttackRoundIndex { get; private set; }

    [Header("Settings")]
    [SerializeField, Min(1)] int threshold = 21;
    [SerializeField] bool reshuffleWhenLow = true;
    [SerializeField, Min(5)] int lowDeckCount = 8;
    [SerializeField] Vector2 enemyDrawDelayRange = new(0.5f, 1.5f);
    [SerializeField, Min(0f)] float enemyAttackSpacing = 1.0f;
    [SerializeField, Min(0f)] float inputDebounceSeconds = 0.12f;

    // --------- Refs ----------
    [Header("Refs")]
    [SerializeField] SimpleCombatant player;
    [SerializeField] List<SimpleCombatant> enemies = new();

    // --------- Bridge/UI ----------
    [Header("UnityEvents (Bridge/UI)")]
    public UnityEvent<Actor, PhaseKind, int, int> onProgress;
    public UnityEvent<Actor, PhaseKind, Card> onCardDrawn;
    public UnityEvent<string> onLog;
    public UnityEvent onGameOver;
    public UnityEvent onGameWin;

    // --------- State Events ----------
    [Header("State Events")]
    public StepEvent onStepChanged;
    public BoolEvent onWaitingForTargetChanged;
    public EnemyIdxEvent onEnemyTurnIndexChanged;
    public EnemyPhaseStartedEvent onEnemyPhaseStarted;
    public EnemyPhaseEndedEvent onEnemyPhaseEnded;
    public UnityEvent onRoundStarted;
    public UnityEvent onRoundResolved;
    public IntEvent onPlayerDefLocked;
    public IntEvent onPlayerAtkLocked;
    public TargetEvent onTargetChanged;
    public IReadOnlyList<SimpleCombatant> AliveEnemies
        => _enemies?.AliveEnemies ?? _enemies?.All ?? new List<SimpleCombatant>();

    // --------- Animation Bridge ----------
    [Header("Animation Events")]
    public UnityEvent<SimpleCombatant, SimpleCombatant, int> onAttackAnimationRequest;

    // --------- Internals ----------
    public CombatContext Ctx { get; private set; }
    public ActionQueue Queue { get; private set; }
    public BattleState State { get; private set; }
    public SimpleCombatant Player => player;
    EnemyRegistry _enemies;
    PlayerPhaseController _player;
    EnemyPhaseController _enemy;
    TargetingController _targeting;
    ResolutionController _resolution;
    InputGate _input;

    [System.Serializable]
    public class UIEvents
    {
        public UnityEvent OnRelicsChanged;
    }

    [Header("Events (UI/Global)")]
    public UIEvents Events = new UIEvents();

    bool _isGameStarted;
    bool _systemsReady;

    // === ICoroutineHost ===
    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

    // === IAnimationBridge ===
    bool _animImpact, _animDone;
    [SerializeField, Min(0f)] float animImpactTimeout = 2f;
    [SerializeField, Min(0f)] float animDoneTimeout = 2f;

    public void AnimReportImpact() => _animImpact = true;
    public void AnimReportDone() => _animDone = true;

    public IEnumerator PlayAttackAnimation(SimpleCombatant attacker, SimpleCombatant defender, int damage, System.Action onImpact)
    {
        _animImpact = false; _animDone = false;
        onAttackAnimationRequest?.Invoke(attacker, defender, damage);

        float t = animImpactTimeout;
        while (!_animImpact && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        onImpact?.Invoke();

        t = animDoneTimeout;
        while (!_animDone && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => EnemySpawner.EnemiesSpawned += OnEnemiesSpawnedEvent;
    void OnDisable() => EnemySpawner.EnemiesSpawned -= OnEnemiesSpawnedEvent;

    IEnumerator Start()
    {
        // Spawner’ların Awake/Start’ı bitsin
        yield return null;

        ResolveRefsInitial();
        BuildContextAndSystems();

        // ▶ Win / GameOver olduğunda HP yakala
        onGameWin.AddListener(CapturePlayerHP);
        onGameOver.AddListener(CapturePlayerHP);

        // ▶ Yeni combat başlarken varsa taşınan HP’yi uygula
        ApplyCarriedHPIfAny();

        if (autoStartOnAwake) StartGame();
        else onLog?.Invoke("Press START to begin.");
    }

    void ResolveRefsInitial()
    {
        // --- Player'ı bul ---
        if (!player) player = FindPlayerSC();

        // --- Enemy listesini kur ---
        var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        enemies = all.Where(e => e && e != player).ToList();

        onLog?.Invoke($"[Init] Player: {(player ? player.name : "NULL")} | Enemies: {enemies.Count}");
    }

    SimpleCombatant FindPlayerSC()
    {
        // 1) Tag ile
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged)
        {
            var sc = tagged.GetComponentInChildren<SimpleCombatant>(true);
            if (sc) return sc;
        }

        // 2) PlayerStats taşıyan SC
        var scs = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var byStats = scs.FirstOrDefault(s => s && s.GetComponentInChildren<PlayerStats>(true));
        if (byStats) return byStats;

        // 3) İsim içerik kontrol (yedek)
        var byName = scs.FirstOrDefault(s => s && s.name.ToLower().Contains("player"));
        if (byName) return byName;

        return null;
    }

    void BuildContextAndSystems()
    {
        var firstEnemy = enemies.Count > 0 ? enemies[0] : null;
        Ctx = new CombatContext(threshold, null, player, firstEnemy);

        Ctx.OnProgress.AddListener((a, k, cur, max) => onProgress?.Invoke(a, k, cur, max));
        Ctx.OnCardDrawn.AddListener((a, k, c) => onCardDrawn?.Invoke(a, k, c));
        Ctx.OnLog.AddListener(s => onLog?.Invoke(s));

        // --- Deck kayıtları ---
        var pDeck = BuildDeckForUnit(player);
        Ctx.RegisterDeck(player, pDeck);

        foreach (var e in enemies.Where(x => x))
        {
            var d = BuildDeckForUnit(e);
            Ctx.RegisterDeck(e, d);
        }

        // Player threshold
        ApplyInitialPlayerThresholds();

        // --- Sistemler ---
        Queue      = new ActionQueue();
        State      = new BattleState();
        _enemies   = new EnemyRegistry(this, player, enemies, onLog);
        _player    = new PlayerPhaseController(this, Ctx, Queue, State, onPlayerDefLocked, onPlayerAtkLocked, onLog);
        _enemy     = new EnemyPhaseController(this, Ctx, Queue, State, onEnemyTurnIndexChanged, onEnemyPhaseStarted, onEnemyPhaseEnded, enemyDrawDelayRange, onLog);
        _targeting = new TargetingController(this, State, _enemies, onWaitingForTargetChanged, onTargetChanged, onLog);
        _resolution = new ResolutionController(this, Ctx, State, _enemies, enemyAttackSpacing, onLog, onRoundResolved, onGameOver, onGameWin, this);
        _input      = new InputGate(inputDebounceSeconds);

        _systemsReady = true;
        ContextReady?.Invoke();

        // MiniBoss pattern logları için enemy phase event'ine bağlan
        onEnemyPhaseEnded.AddListener(HandleEnemyPhaseEndedForMiniBoss);

        onLog?.Invoke($"[Decks] PlayerDeck={Ctx.GetDeckFor(Actor.Player)?.Count ?? 0} | EnemyDecks={enemies.Count}");
    }

    void ApplyInitialPlayerThresholds()
    {
        if (!player) return;

        var stats = player.GetComponentInChildren<PlayerStats>(true);
        if (stats)
        {
            Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack, stats.MaxAttackRange);
            Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, stats.MaxDefenseRange);
            onLog?.Invoke($"[Init] Player thresholds set from stats → ATK:{stats.MaxAttackRange} DEF:{stats.MaxDefenseRange}");
        }
        else
        {
            onLog?.Invoke("[Init] PlayerStats not found on player. Using fallback/global threshold.");
        }
    }

    public void ResetCombatState()
    {
        Log("[Director] Resetting combat state...");

        TurnIndex = 0;
        EnemyAttackRoundIndex = 0;

        State = new BattleState();
        Queue = new ActionQueue();

        // Deckleri yeniden kur
        if (player)
            Ctx?.RegisterDeck(player, BuildDeckForUnit(player));

        if (_enemies != null)
        {
            foreach (var e in _enemies.All.Where(x => x))
                Ctx?.RegisterDeck(e, BuildDeckForUnit(e));
        }

        _player?.ResetAccumulator(PhaseKind.Defense);
        _player?.ResetAccumulator(PhaseKind.Attack);

        _targeting?.CancelTargetMode();

        ApplyInitialPlayerThresholds();

        State.ResetForNewTurn();

        Log("[Director] Combat state reset complete.");
    }

    IDeckService BuildDeckForUnit(SimpleCombatant unit)
    {
        if (unit)
        {
            var def = unit.GetComponent<CombatantDeck>();
            if (def != null)
            {
                var built = def.BuildDeck();
                onLog?.Invoke($"[Deck] {unit.name} deck built → {built?.Count ?? 0} cards.");
                return built ?? new DeckService();
            }
        }

        var deck = new DeckService();
        var initial = CreateDefault52();
        deck.SetInitialCards(initial, takeSnapshot: true);
        deck.Shuffle();

        onLog?.Invoke($"[Deck] Fallback deck built for {(unit ? unit.name : "NULL")} → {deck.Count} cards.");
        return deck;
    }

    List<Card> CreateDefault52()
    {
        var list = new List<Card>(52);
        string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades" };

        foreach (var s in suits)
        {
            for (int v = 2; v <= 10; v++) list.Add(new Card((Rank)v, s));
            list.Add(new Card(Rank.Jack, s));
            list.Add(new Card(Rank.Queen, s));
            list.Add(new Card(Rank.King, s));
            list.Add(new Card(Rank.Ace, s));
        }
        return list;
    }

    void OnEnemiesSpawnedEvent()
    {
        if (!_systemsReady || _enemies == null)
        {
            var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            enemies = all.Where(e => e && e != player).ToList();
            onLog?.Invoke($"[Director] (early) enemies detected before init: {enemies.Count}");
            return;
        }

        _enemies.Refresh();
        onLog?.Invoke($"[Director] Enemies refreshed. Count={_enemies.AliveEnemies.Count}");
    }

    // === External API (UI) ===
    public void StartGame()
    {
        if (_isGameStarted) return;

        TurnIndex = 0;
        EnemyAttackRoundIndex = 0;

        _isGameStarted = true;
        onGameStarted?.Invoke();
        StartNewTurn();
    }

    /// <summary>CombatFlowAdapter gibi yerlerden çağır: Minor / Elite / Boss.</summary>
    public void SetFightKind(FightKind kind)
    {
        CurrentFightKind = kind;
        Log($"[Director] FightKind set to {kind}");
    }

    public void OnDrawClicked()
    {
        if (!_guardInput()) return;

        switch (State.Step)
        {
            case TurnStep.PlayerDef: Run(_player.DrawDefense()); break;
            case TurnStep.PlayerAtk: Run(_player.DrawAttack()); break;
            default: onLog?.Invoke("Draw is not available in this step."); break;
        }
    }

    public void OnAcceptClicked()
    {
        if (!_guardInput()) return;

        switch (State.Step)
        {
            case TurnStep.PlayerDef: Run(_player.AcceptDefense(() => BeginPhase(TurnStep.PlayerAtk))); break;
            case TurnStep.PlayerAtk: Run(_player.AcceptAttack(() => BeginPhase(TurnStep.SelectTarget))); break;
            default: onLog?.Invoke("Accept is not available in this step."); break;
        }
    }

    public void SelectTarget(SimpleCombatant enemy)
    {
        if (_targeting.TrySelectTarget(enemy))
            Run(_resolution.ResolveRoundAndRestart());
    }

    public void SetThreshold(int value)
    {
        Ctx.SetGlobalThreshold(Mathf.Max(5, value), refreshProgress: false);
        Ctx.OnLog?.Invoke($"[Rule] Threshold (global fallback) → {Ctx.Threshold}");

        var pDef = Ctx.TryGetAcc(Actor.Player, PhaseKind.Defense, false, true);
        if (pDef != null)
            Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Defense, pDef.Total, Ctx.GetThreshold(Actor.Player, PhaseKind.Defense));

        var pAtk = Ctx.TryGetAcc(Actor.Player, PhaseKind.Attack, false, true);
        if (pAtk != null)
            Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Attack, pAtk.Total, Ctx.GetThreshold(Actor.Player, PhaseKind.Attack));

        var eDef = Ctx.TryGetAcc(Actor.Enemy, PhaseKind.Defense, false, false);
        if (eDef != null)
            Ctx.OnProgress?.Invoke(Actor.Enemy, PhaseKind.Defense, eDef.Total, Ctx.GetThreshold(Actor.Enemy, PhaseKind.Defense));

        var eAtk = Ctx.TryGetAcc(Actor.Enemy, PhaseKind.Attack, false, false);
        if (eAtk != null)
            Ctx.OnProgress?.Invoke(Actor.Enemy, PhaseKind.Attack, eAtk.Total, Ctx.GetThreshold(Actor.Enemy, PhaseKind.Attack));
    }

    public void Log(string msg)
    {
        Debug.Log($"[GameDirector] {msg}");
        onLog?.Invoke(msg);
    }

    public int GetThresholdSafe() => Ctx != null ? Ctx.Threshold : threshold;

    // === Turn flow ===
    public void StartNewTurn()
    {
        if (!_isGameStarted) return;

        TurnIndex++;
        EnemyAttackRoundIndex = 0;

        onRoundStarted?.Invoke();

        RelicManager.Instance?.OnTurnStart();

        State.ResetForNewTurn();
        Queue.Enqueue(new StartTurnAction(reshuffleWhenLow, lowDeckCount));
        Run(Queue.RunAllCoroutine(Ctx));

        if (_enemy.Running != null) StopCoroutine(_enemy.Running);
        _enemy.Running = StartCoroutine(
            _enemy.PrecomputeBothPhasesThen(() => BeginPhase(TurnStep.PlayerDef))
        );
    }

    /// <summary>
    /// Resolve sırasında enemy saldırı eli sayacını 1 arttırır ve yeni değeri döner.
    /// MiniBoss / Boss pattern’leri bu değeri kullanır.
    /// </summary>
    public int NextEnemyAttackRound()
    {
        EnemyAttackRoundIndex++;
        return EnemyAttackRoundIndex;
    }

    public void SetPlayerPhaseThresholds(int atk, int def)
    {
        if (Ctx == null) return;
        Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Attack, atk);
        Ctx.SetPhaseThreshold(Actor.Player, PhaseKind.Defense, def);
    }

    public void SetEnemyPhaseThresholds(int atk, int def)
    {
        if (Ctx == null) return;
        Ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Attack, atk);
        Ctx.SetPhaseThreshold(Actor.Enemy, PhaseKind.Defense, def);
    }

    public void BeginPhase(TurnStep step)
    {
        State.SetStep(step, onStepChanged, onLog);

        switch (step)
        {
            case TurnStep.PlayerDef:
                _targeting.CancelTargetMode();
                _player.ResetAccumulator(PhaseKind.Defense);
                break;

            case TurnStep.PlayerAtk:
                _targeting.CancelTargetMode();
                _player.ResetAccumulator(PhaseKind.Attack);
                break;

            case TurnStep.SelectTarget:
                _targeting.BeginTargetMode(State.PlayerAtkTotal, TryAutoTargetSingle: true);

                if (!State.WaitingForTarget && State.CurrentTarget != null)
                {
                    Run(_resolution.ResolveRoundAndRestart());
                    return;
                }
                break;
        }
    }

    void CapturePlayerHP()
    {
        var hm = player ? player.GetComponent<HealthManager>() : null;
        if (hm != null)
        {
            s_LastPlayerHP = hm.CurrentHP;
            onLog?.Invoke($"[CarryHP] Saved HP = {s_LastPlayerHP}/{hm.MaxHP}");
        }
    }

    void ApplyCarriedHPIfAny()
    {
        var hm = player ? player.GetComponent<HealthManager>() : null;
        if (hm == null) return;

        if (s_LastPlayerHP.HasValue)
        {
            int target = Mathf.Clamp(s_LastPlayerHP.Value, 1, hm.MaxHP);
            hm.SetHP(target);
            onLog?.Invoke($"[CarryHP] Applied HP = {target}/{hm.MaxHP}");
        }
        else
        {
            onLog?.Invoke("[CarryHP] No carried HP found. Using scene value.");
        }
    }

    public void ResolveNow()
    {
        Run(_resolution.ResolveRoundAndRestart());
    }

    // === MiniBoss pattern handler (bilgi amaçlı log) ===
    void HandleEnemyPhaseEndedForMiniBoss(SimpleCombatant enemy, PhaseKind phase, int total)
    {
        if (phase != PhaseKind.Attack) return;
        if (enemy == null || Ctx == null) return;

        var mini = enemy.GetComponent<MiniBossRuntime>();
        if (mini == null || mini.Definition == null || mini.Definition.attackBehaviour == null)
            return; // normal enemy → pattern yok

        Log($"[MiniBoss] Enemy ATK phase ended. baseATK={total}, currentRoundIndex={EnemyAttackRoundIndex}");
    }

    // === helpers ===
    bool _guardInput()
    {
        if (!_isGameStarted) { onLog?.Invoke("Press START to begin."); return false; }
        if (State.IsBusy || State.WaitingForTarget) { onLog?.Invoke("Please wait..."); return false; }
        if (!_input.AllowClick()) return false;
        return true;
    }
}
