using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public enum TurnStep { PlayerDef, PlayerAtk, SelectTarget, EnemyDef, EnemyAtk, Resolve }

// === State Event Types (aynen korunur) ===
[System.Serializable] public class StepEvent : UnityEvent<TurnStep> {}
[System.Serializable] public class BoolEvent : UnityEvent<bool> {}
[System.Serializable] public class EnemyIdxEvent : UnityEvent<SimpleCombatant,int> {}
[System.Serializable] public class EnemyPhaseStartedEvent : UnityEvent<SimpleCombatant, PhaseKind> {}
[System.Serializable] public class EnemyPhaseEndedEvent   : UnityEvent<SimpleCombatant, PhaseKind, int> {}
[System.Serializable] public class IntEvent : UnityEvent<int> {}
[System.Serializable] public class TargetEvent : UnityEvent<SimpleCombatant> {}

[DisallowMultipleComponent]
public class GameDirector : MonoBehaviour, ICoroutineHost, IAnimationBridge
{
    public static event System.Action ContextReady;
    // --------- Singleton ----------
    public static GameDirector Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoStartOnAwake = false;
    public UnityEvent onGameStarted;

    [Header("Settings")]
    [SerializeField, Min(1)] int threshold = 21;
    [SerializeField] bool reshuffleWhenLow = true;
    [SerializeField, Min(5)] int lowDeckCount = 8;
    [SerializeField] Vector2 enemyDrawDelayRange = new(0.5f, 1.5f);
    [SerializeField, Min(0f)] float enemyAttackSpacing = 1.0f; // düşmanlar arası bekleme
    [SerializeField, Min(0f)] float inputDebounceSeconds = 0.12f;

    // --------- Refs ----------
    [Header("Refs")]
    [SerializeField] SimpleCombatant player;
    [SerializeField] List<SimpleCombatant> enemies = new();

    // --------- Bridge/UI ----------
    [Header("UnityEvents (Bridge/UI)")]
    public UnityEvent<Actor,PhaseKind,int,int> onProgress;
    public UnityEvent<Actor,PhaseKind,Card> onCardDrawn;
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

    // --------- Animation Bridge (UI/Animator tarafı bağlanır) ----------
    [Header("Animation Events")]
    public UnityEvent<SimpleCombatant, SimpleCombatant, int> onAttackAnimationRequest; // (attacker, defender, damage)

    // --------- Internals ----------
    public CombatContext Ctx { get; private set; }
    public ActionQueue Queue { get; private set; }
    public BattleState State { get; private set; }

    EnemyRegistry _enemies;
    PlayerPhaseController _player;
    EnemyPhaseController _enemy;
    TargetingController _targeting;
    ResolutionController _resolution;
    InputGate _input;

    bool _isGameStarted;
    bool _systemsReady;  // Context + registry + controller'lar kuruldu mu?     
    // === ICoroutineHost ===
    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

    // === IAnimationBridge ===
    bool _animImpact, _animDone;
    [SerializeField, Min(0f)] float animImpactTimeout = 2f;
    [SerializeField, Min(0f)] float animDoneTimeout   = 2f;
    public void AnimReportImpact() => _animImpact = true;
    public void AnimReportDone()   => _animDone = true;
    public IEnumerator PlayAttackAnimation(SimpleCombatant attacker, SimpleCombatant defender, int damage, System.Action onImpact)
    {
        _animImpact = false; _animDone = false;
        onAttackAnimationRequest?.Invoke(attacker, defender, damage);

        float t = animImpactTimeout;
        while (!_animImpact && t > 0f) { t -= Time.deltaTime; yield return null; }
        onImpact?.Invoke();

        t = animDoneTimeout;
        while (!_animDone && t > 0f) { t -= Time.deltaTime; yield return null; }
    }

void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
}

    void OnEnable()  => EnemySpawner.EnemiesSpawned += OnEnemiesSpawnedEvent;
    void OnDisable() => EnemySpawner.EnemiesSpawned -= OnEnemiesSpawnedEvent;

    IEnumerator Start()
    {
        // Spawner’ların Awake/Start’ı bitsin
        yield return null; // 1 frame bekle

        ResolveRefsInitial();    // <- player & enemies doğru kur
        BuildContextAndSystems(); // <- Ctx, Queue, Controller’lar

        if (autoStartOnAwake) StartGame(); else onLog?.Invoke("Press START to begin.");
    }
    void ResolveRefsInitial()
    {
        // --- Player'ı bul ---
        if (!player) player = FindPlayerSC();

        // --- Enemy listesini kur ---
        var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        enemies = all.Where(e => e && e != player).ToList();

        // Bilgi
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

        // 2) PlayerStats taşıyan SC (en güvenilir)
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

        // Player deck
        var pDeck = BuildDeckForUnit(player);
        Ctx.RegisterDeck(player, pDeck);

        // Enemy deck’leri
        foreach (var e in enemies.Where(x => x))
        {
            var eDeck = BuildDeckForUnit(e);
            Ctx.RegisterDeck(e, eDeck);
        }

        Queue = new ActionQueue();
        State = new BattleState();
        _enemies = new EnemyRegistry(this, player, enemies, onLog);
        _player = new PlayerPhaseController(this, Ctx, Queue, State,
                                            onPlayerDefLocked, onPlayerAtkLocked, onLog);
        _enemy = new EnemyPhaseController(this, Ctx, Queue, State,
                                            onEnemyTurnIndexChanged, onEnemyPhaseStarted, onEnemyPhaseEnded,
                                            enemyDrawDelayRange, onLog);
        _targeting = new TargetingController(this, State, _enemies, onWaitingForTargetChanged, onTargetChanged, onLog);
        _resolution = new ResolutionController(this, Ctx, State, _enemies,
                                            enemyAttackSpacing, onLog, onRoundResolved,
                                            onGameOver, onGameWin, this);
        _input = new InputGate(inputDebounceSeconds);
        
        ContextReady?.Invoke();
        _systemsReady = true;

    }
    IDeckService BuildDeckForUnit(SimpleCombatant unit)
    {
        if (!unit) return new DeckService(); // default

        var def = unit.GetComponent<CombatantDeck>();
        if (def != null) return def.BuildDeck();

        // FallBack: boş default deste
        var d = new DeckService();
        d.RebuildAndShuffle();
         Debug.Log("sa2");
        return d;
    }
    void OnEnemiesSpawnedEvent()
    {
        // Sistemler daha hazır değilse, sadece sahnedeki enemy referanslarını topla ve çık
        if (!_systemsReady || _enemies == null)
        {
            var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            enemies = all.Where(e => e && e != player).ToList();
            onLog?.Invoke($"[Director] (early) enemies detected before init: {enemies.Count}");
            return;
        }

        // Normal yol
        _enemies.Refresh();
        onLog?.Invoke($"[Director] Enemies refreshed. Count={_enemies.AliveEnemies.Count}");
    }
    // === External API (UI) ===
    public void StartGame()
    {
        if (_isGameStarted) return;
        _isGameStarted = true;
        onGameStarted?.Invoke();
        StartNewTurn();
    }

    public void OnDrawClicked()
    {
        if (!_guardInput()) return;

        switch (State.Step)
        {
            case TurnStep.PlayerDef: Run(_player.DrawDefense()); break;
            case TurnStep.PlayerAtk: Run(_player.DrawAttack());  break;
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
        Ctx.Threshold = Mathf.Max(5, value);
        Ctx.OnLog?.Invoke($"[Rule] Threshold → {Ctx.Threshold}");

        // Player için
        var pDef = Ctx.TryGetAcc(Actor.Player, PhaseKind.Defense, createIfMissing:false, isPlayerFlagIfCreate:true);
        if (pDef != null) Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Defense, pDef.Total, Ctx.Threshold);

        var pAtk = Ctx.TryGetAcc(Actor.Player, PhaseKind.Attack,  createIfMissing:false, isPlayerFlagIfCreate:true);
        if (pAtk != null) Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Attack,  pAtk.Total, Ctx.Threshold);

        // Enemy için (varsa)
        var eDef = Ctx.TryGetAcc(Actor.Enemy, PhaseKind.Defense, createIfMissing:false, isPlayerFlagIfCreate:false);
        if (eDef != null) Ctx.OnProgress?.Invoke(Actor.Enemy, PhaseKind.Defense, eDef.Total, Ctx.Threshold);

        var eAtk = Ctx.TryGetAcc(Actor.Enemy, PhaseKind.Attack,  createIfMissing:false, isPlayerFlagIfCreate:false);
        if (eAtk != null) Ctx.OnProgress?.Invoke(Actor.Enemy, PhaseKind.Attack,  eAtk.Total, Ctx.Threshold);
    }
    public int GetThresholdSafe()
    {
        return Ctx != null ? Ctx.Threshold : threshold;
    }
    // === Turn flow ===
    public void StartNewTurn()
    {
        if (!_isGameStarted) return;
        onRoundStarted?.Invoke();

        State.ResetForNewTurn();
        Queue.Enqueue(new StartTurnAction(reshuffleWhenLow, lowDeckCount));
        Run(Queue.RunAllCoroutine(Ctx));

        if (_enemy.Running != null) StopCoroutine(_enemy.Running);
        _enemy.Running = StartCoroutine(_enemy.PrecomputeBothPhasesThen(() => BeginPhase(TurnStep.PlayerDef)));
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
                // Tek düşman varsa otomatik seç; yoksa beklemeye geç
                _targeting.BeginTargetMode(State.PlayerAtkTotal, TryAutoTargetSingle: true);

                // ⬇️ OTOMATİK HEDEF seçildiyse (Waiting=false ve CurrentTarget dolu) hemen çöz
                if (!State.WaitingForTarget && State.CurrentTarget != null)
                {
                    Run(_resolution.ResolveRoundAndRestart());
                    return; // çözüm akışı faz değişimini yönetecek
                }
                break;
        }
    }
    public void ResolveNow()
    {
        Run(_resolution.ResolveRoundAndRestart());
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
