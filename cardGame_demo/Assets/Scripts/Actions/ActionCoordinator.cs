using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TurnStep { PlayerDef, PlayerAtk, EnemyDef, EnemyAtk, Resolve }

// === State Event Types ===
[System.Serializable] public class StepEvent : UnityEvent<TurnStep> {}
[System.Serializable] public class BoolEvent : UnityEvent<bool> {}
[System.Serializable] public class EnemyIdxEvent : UnityEvent<SimpleCombatant,int> {}
[System.Serializable] public class EnemyPhaseStartedEvent : UnityEvent<SimpleCombatant, PhaseKind> {}
[System.Serializable] public class EnemyPhaseEndedEvent   : UnityEvent<SimpleCombatant, PhaseKind, int> {}
[System.Serializable] public class IntEvent : UnityEvent<int> {}
[System.Serializable] public class TargetEvent : UnityEvent<SimpleCombatant> {}

[DisallowMultipleComponent]
public class ActionCoordinator : MonoBehaviour
{
    // --------- Singleton ----------
    public static ActionCoordinator Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField, Min(0f)] float enemyAttackSpacing = 1.0f; // düşman saldırıları arası ek bekleme
    [SerializeField] private bool autoStartOnAwake = false;
    public UnityEvent onGameStarted;
    private bool isGameStarted = false;

    // --------- Settings ----------
    [Header("Settings")]
    [SerializeField, Min(1)] int threshold = 21;
    [SerializeField] bool reshuffleWhenLow = true;
    [SerializeField, Min(5)] int lowDeckCount = 8;
    [SerializeField] Vector2 enemyDrawDelayRange = new Vector2(0.5f, 1.5f);
    public int GetThresholdSafe() => ctx != null ? ctx.Threshold : threshold;

    // --------- Refs ----------
    [Header("Refs")]
    [SerializeField] SimpleCombatant player;
    [SerializeField] List<SimpleCombatant> enemies = new List<SimpleCombatant>();

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

    // --------- Internal state ----------
    CombatContext ctx;
    ActionQueue queue;
    TurnStep step;

    bool isBusy;
    bool waitingForTarget;
    SimpleCombatant currentTarget;

    readonly Dictionary<SimpleCombatant, int> enemyDefTotals = new();
    readonly Dictionary<SimpleCombatant, int> enemyAtkTotals = new();

    int playerDefTotal;
    int playerAtkTotal;

    [Header("Animation Events")]
    public UnityEvent<SimpleCombatant, SimpleCombatant, int> onAttackAnimationRequest; // (attacker, defender, damage)

    // Animator'ın çağıracağı geri bildirimler:
    private bool _animImpact;
    private bool _animDone;

    public void AnimReportImpact()  { _animImpact = true; }
    public void AnimReportDone()    { _animDone   = true; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // enemies init + ctx init vs...
        if (enemies == null || enemies.Count == 0)
        {
            var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            enemies = all.Where(e => e != player).ToList();
        }

        var firstEnemy = enemies.Count > 0 ? enemies[0] : null;
        ctx = new CombatContext(threshold, new DeckService(), player, firstEnemy);

        ctx.OnProgress.AddListener((a,k,cur,max)=> onProgress?.Invoke(a,k,cur,max));
        ctx.OnCardDrawn.AddListener((a,k,c)=> onCardDrawn?.Invoke(a,k,c));
        ctx.OnLog.AddListener(s=> onLog?.Invoke(s));
        queue = new ActionQueue();

        // ÖNCEKİ: StartNewTurn();  --> KALDIRILDI
        if (autoStartOnAwake) StartGame();
        else onLog?.Invoke("Press START to begin.");
    }
    public void StartGame()
    {
        if (isGameStarted) return;
        isGameStarted = true;
        onGameStarted?.Invoke();
        StartNewTurn();
    }
    void OnEnable()
    {
        // Spawner'ların global eventine abone ol
        EnemySpawner.EnemiesSpawned += OnEnemiesSpawnedEvent;
    }

    void OnDisable()
    {
        EnemySpawner.EnemiesSpawned -= OnEnemiesSpawnedEvent;
    }
    void OnEnemiesSpawnedEvent()
    {
        RefreshEnemyListFromScene();
        onLog?.Invoke($"[Coordinator] Enemies refreshed. Count={enemies.Count}");
    }

    // === Helpers (tek yerden event) ===
    void SetStep(TurnStep s) { step = s; onStepChanged?.Invoke(step); AnnounceStep(); }
    void SetWaitingForTarget(bool v) { if (waitingForTarget == v) { waitingForTarget = v; return; } waitingForTarget = v; onWaitingForTargetChanged?.Invoke(waitingForTarget); }
    bool TryAutoSelectSingleEnemy()
    {
        // yalnizca canlı düşmanları say
        var alive = enemies.Where(e => e && e.CurrentHP > 0).ToList();
        if (alive.Count == 1 && waitingForTarget && step == TurnStep.PlayerAtk)
        {
            // SelectTargetSafe zaten gerekli kontrolleri yapıyor ve akışı başlatıyor
            return SelectTargetSafe(alive[0]);
        }
        return false;
    }

    void StartNewTurn()
    {
        if (!isGameStarted) return;

        onRoundStarted?.Invoke();

        SetWaitingForTarget(false);
        currentTarget = null;
        playerDefTotal = 0;
        playerAtkTotal = 0;
        enemyDefTotals.Clear();
        enemyAtkTotals.Clear();

        queue.Enqueue(new StartTurnAction(reshuffleWhenLow, lowDeckCount));
        StartCoroutine(queue.RunAllCoroutine(ctx));


        if (enemyTurnCo != null) StopCoroutine(enemyTurnCo);
        enemyTurnCo = StartCoroutine(EnemiesPrecomputeTwoPhasesRoutine());
    }
    IEnumerator EnemiesPrecomputeTwoPhasesRoutine()
    {
        isBusy = true;

        if (enemies.Count == 0)
        {
            onLog?.Invoke("No enemies.");
            // Düşman yoksa direkt oyuncuya geç
            BeginPhase(TurnStep.PlayerDef);
            isBusy = false;
            yield break;
        }

        // --- Enemy DEF ---
        SetStep(TurnStep.EnemyDef);
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e) continue;

            SwapCtxEnemy(e);
            onEnemyTurnIndexChanged?.Invoke(e, i);
            onEnemyPhaseStarted?.Invoke(e, PhaseKind.Defense);

            yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Defense));
            int totalDef = ctx.GetAcc(Actor.Enemy, PhaseKind.Defense).Total;
            enemyDefTotals[e] = totalDef;
            onEnemyPhaseEnded?.Invoke(e, PhaseKind.Defense, totalDef);
        }

        // --- Enemy ATK ---
        SetStep(TurnStep.EnemyAtk);
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e) continue;

            SwapCtxEnemy(e);
            onEnemyTurnIndexChanged?.Invoke(e, i);
            onEnemyPhaseStarted?.Invoke(e, PhaseKind.Attack);

            yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Attack));
            int atk = ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;
            enemyAtkTotals[e] = atk;
            e.CurrentAttack = atk;
            onEnemyPhaseEnded?.Invoke(e, PhaseKind.Attack, atk);
        }

        // Düşman değerleri hazır → oyuncu turlarına geç
        isBusy = false;
        BeginPhase(TurnStep.PlayerDef);
    }

    void AnnounceStep()
    {
        onLog?.Invoke(step switch
        {
            TurnStep.PlayerDef => "Your Defense: Draw or Accept.",
            TurnStep.PlayerAtk => "Your Attack: Draw or Accept.",
            TurnStep.EnemyDef  => "Enemies choosing Defense (in order)...",
            TurnStep.EnemyAtk  => "Enemies choosing Attack (in order)...",
            TurnStep.Resolve   => "Resolving...",
            _ => ""
        });
    }

    void BeginPhase(TurnStep newStep)
    {
        SetStep(newStep);

        if (step == TurnStep.PlayerDef)
        {
            queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Defense));
            StartCoroutine(queue.RunAllCoroutine(ctx));
            if (ctx.GetAcc(Actor.Player, PhaseKind.Defense).IsBusted) GoNextFromPlayerDef();
        }
        else if (step == TurnStep.PlayerAtk)
        {
            queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack));
            StartCoroutine(queue.RunAllCoroutine(ctx));
            if (ctx.GetAcc(Actor.Player, PhaseKind.Attack).IsBusted) GoNextFromPlayerAtk();
        }
    }

    // === UI: Draw / Accept ===
    public void OnDrawClicked()
    {
        if (!isGameStarted) { onLog?.Invoke("Press START to begin."); return; }
        if (isBusy || waitingForTarget) { onLog?.Invoke("Please wait..."); return; }

        switch (step)
        {
            case TurnStep.PlayerDef:
                queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Defense));
                StartCoroutine(queue.RunAllCoroutine(ctx));
                if (ctx.GetAcc(Actor.Player, PhaseKind.Defense).IsBusted) GoNextFromPlayerDef();
                break;

            case TurnStep.PlayerAtk:
                queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack));
                StartCoroutine(queue.RunAllCoroutine(ctx));
                if (ctx.GetAcc(Actor.Player, PhaseKind.Attack).IsBusted) GoNextFromPlayerAtk();
                break;

            default:
                onLog?.Invoke("Draw is not available in this step.");
                break;
        }
    }
    public void OnAcceptClicked()
    {
        if (!isGameStarted) { onLog?.Invoke("Press START to begin."); return; }
        if (isBusy || waitingForTarget) { onLog?.Invoke("Please wait..."); return; }

        switch (step)
        {
            case TurnStep.PlayerDef:
                queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Defense));
                StartCoroutine(queue.RunAllCoroutine(ctx));
                playerDefTotal = ctx.GetAcc(Actor.Player, PhaseKind.Defense).Total;
                onPlayerDefLocked?.Invoke(playerDefTotal);
                BeginPhase(TurnStep.PlayerAtk);
                break;

            case TurnStep.PlayerAtk:
                queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Attack));
                StartCoroutine(queue.RunAllCoroutine(ctx));
                playerAtkTotal = ctx.GetAcc(Actor.Player, PhaseKind.Attack).Total;
                player.CurrentAttack = playerAtkTotal;
                onPlayerAtkLocked?.Invoke(playerAtkTotal);

                SetWaitingForTarget(true);
                onLog?.Invoke($"[SelectTarget] Your ATK={playerAtkTotal}. Click an enemy to target.");

                // Tek düşmansa otomatik seç
                if (TryAutoSelectSingleEnemy())
                {
                    onLog?.Invoke("[SelectTarget] Single enemy detected. Auto-targeted.");
                    // Auto-seçim SelectTargetSafe içinden Resolve'ı başlatacak (aşağıda)
                }
                break;

            default:
                onLog?.Invoke("Accept is not available in this step.");
                break;
        }
    }

    void GoNextFromPlayerDef()
    {
        playerDefTotal = 0;
        BeginPhase(TurnStep.PlayerAtk);
    }

    void GoNextFromPlayerAtk()
    {
        playerAtkTotal = 0;
        player.CurrentAttack = 0;

        SetWaitingForTarget(false);
        // ESKİ: düşman fazlarına geçiyordu
        // YENİ: düşmanlar zaten hazır → direkt resolve
        StartCoroutine(ResolveAllAndCleanup());
    }


    Coroutine enemyTurnCo;

    public bool SelectTargetSafe(SimpleCombatant enemy)
    {
        if (!waitingForTarget || step != TurnStep.PlayerAtk)
        {
            onLog?.Invoke("[SelectTarget] Not expecting a target now.");
            return false;
        }
        if (enemy == null || !enemies.Contains(enemy))
        {
            onLog?.Invoke("[SelectTarget] Invalid enemy.");
            return false;
        }

        currentTarget = enemy;
        onTargetChanged?.Invoke(currentTarget);
        SetWaitingForTarget(false);

        // DÜZELTİLDİ: ESKİDE burada düşman fazlarını başlatıyorduk.
        // Artık düşman değerleri hazır; direkt resolve.
        StartCoroutine(ResolveAllAndCleanup());
        return true;
    }
    public void SelectTarget(SimpleCombatant enemy) => SelectTargetSafe(enemy);

    IEnumerator EnemiesAutoTwoPhasesRoutine()
    {
        isBusy = true;

        if (enemies.Count == 0)
        {
            onLog?.Invoke("No enemies.");
            yield return ResolveAllAndCleanup();
            isBusy = false;
            yield break;
        }

        // Enemy DEF
        SetStep(TurnStep.EnemyDef);
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e) continue;

            SwapCtxEnemy(e);
            onEnemyTurnIndexChanged?.Invoke(e, i);
            onEnemyPhaseStarted?.Invoke(e, PhaseKind.Defense);

            yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Defense));
            int totalDef = ctx.GetAcc(Actor.Enemy, PhaseKind.Defense).Total;
            enemyDefTotals[e] = totalDef;
            onEnemyPhaseEnded?.Invoke(e, PhaseKind.Defense, totalDef);
        }

        // Enemy ATK
        SetStep(TurnStep.EnemyAtk);
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e) continue;

            SwapCtxEnemy(e);
            onEnemyTurnIndexChanged?.Invoke(e, i);
            onEnemyPhaseStarted?.Invoke(e, PhaseKind.Attack);

            yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Attack));
            int atk = ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;
            enemyAtkTotals[e] = atk;
            e.CurrentAttack = atk;
            onEnemyPhaseEnded?.Invoke(e, PhaseKind.Attack, atk);
        }

        // Resolve
        SetStep(TurnStep.Resolve);
        yield return ResolveAllAndCleanup();
        onRoundResolved?.Invoke();

        isBusy = false;
    }

    IEnumerator RunEnemyPhaseWithDelays(PhaseKind phase)
    {
        var enumerator = EnemyPolicy.BuildPhaseEnumerator(ctx, phase);

        while (enumerator.MoveNext())
        {
            var action = enumerator.Current;
            queue.Enqueue(action);
            yield return StartCoroutine(queue.RunAllCoroutine(ctx));

            if (action is DrawCardAction)
            {
                float delay = Random.Range(enemyDrawDelayRange.x, enemyDrawDelayRange.y);
                yield return new WaitForSeconds(delay);
            }
        }

        ctx.OnLog?.Invoke($"[AI] Enemy {phase} done: {ctx.GetAcc(Actor.Enemy, phase).Total}");
    }
    public void RefreshEnemyListFromScene()
    {
        var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var list = new List<SimpleCombatant>();
        foreach (var sc in all)
        {
            if (!sc) continue;
            if (player && sc == player) continue;
            if (sc.CurrentHP <= 0) continue;   // ölüleri alma (isteğe bağlı)
            list.Add(sc);
        }

        // >>> SIRALAMA: spawn index'e göre artan (1→2→3)
        list.Sort((a,b) => GetSpawnOrderForEnemy(a).CompareTo(GetSpawnOrderForEnemy(b)));

        enemies = list;

        // CombatContext’e aktif düşman olarak ilkini koy (varsa)
        var firstEnemy = enemies.Count > 0 ? enemies[0] : null;
        if (firstEnemy != null)
            ctx.SetEnemy(firstEnemy);

        onLog?.Invoke($"[Coordinator] Enemies refreshed (ordered by spawn index). Count={enemies.Count}");
    }
    int GetSpawnOrderForEnemy(SimpleCombatant sc)
    {
        if (!sc) return int.MaxValue;
        var meta = sc.GetComponent<EnemySpawnMeta>();
        if (meta && meta.spawnIndex >= 0) return meta.spawnIndex;

        // Meta yoksa en sona at (istersen en yakın spawnPoint'e göre tahmin de yapabiliriz)
        return int.MaxValue;
    }
    IEnumerator ResolveAllAndCleanup()
    {
        // --- 1) PLAYER -> TARGET (önce oyuncu vurur) ---
        if (currentTarget != null && currentTarget.CurrentHP > 0)
        {
            int targetDef = enemyDefTotals.TryGetValue(currentTarget, out var d) ? Mathf.Max(0, d) : 0;
            int playerDamage = Mathf.Max(0, Mathf.Max(0, playerAtkTotal) - targetDef);

            yield return StartCoroutine(PlayAttackSequence(player, currentTarget, playerDamage, () =>
            {
                if (playerDamage > 0)
                {
                    currentTarget.CurrentHP -= playerDamage;
                    onLog?.Invoke($"You dealt {playerDamage} to {currentTarget.name}.");
                }
                else
                {
                    onLog?.Invoke($"Your attack couldn’t pierce {currentTarget.name}'s defense.");
                }
            }));
        }
        else
        {
            onLog?.Invoke("No valid target for your attack.");
        }

        // --- 2) ENEMIES -> PLAYER (sonra düşmanlar sırayla, aralıklı) ---
        int remainingDef = Mathf.Max(0, playerDefTotal);

        // Son aktif düşmanın indeksini bul (spacing'i en sondan sonra koymamak için)
        int lastActiveIdx = -1;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e && e.CurrentHP > 0) { lastActiveIdx = i; break; }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e || e.CurrentHP <= 0) continue;

            int enemyAtk = enemyAtkTotals.TryGetValue(e, out var atkVal) ? Mathf.Max(0, atkVal) : 0;

            if (enemyAtk > 0)
            {
                int effective = Mathf.Max(0, enemyAtk - remainingDef);
                remainingDef = Mathf.Max(0, remainingDef - enemyAtk);

                yield return StartCoroutine(PlayAttackSequence(e, player, effective, () =>
                {
                    if (effective > 0)
                    {
                        player.CurrentHP -= effective;
                        onLog?.Invoke($"{e.name} hits you for {effective}.");
                    }
                    else
                    {
                        onLog?.Invoke($"{e.name}'s attack was blocked.");
                    }
                }));
            }
            else
            {
                // İstersen boş saldırıda da küçük bir anim oynatabilirsin:
                // yield return StartCoroutine(PlayAttackSequence(e, player, 0));
                onLog?.Invoke($"{e.name} attacks but has no effective attack.");
            }

            // --- Araya bekleme ekle (son aktif düşmandan sonra bekleme yok) ---
            if (i < lastActiveIdx && enemyAttackSpacing > 0f)
                yield return new WaitForSeconds(enemyAttackSpacing);
        }

        // --- Win/Lose kontrolü ---
        if (CheckWinLose()) yield break;

        // --- Temizlik ve yeni el ---
        player.CurrentAttack = 0;
        foreach (var e in enemies) if (e) e.CurrentAttack = 0;

        StartNewTurn();
        yield return null;
    }

    bool CheckWinLose()
    {
        enemies.RemoveAll(e => e == null);
        var aliveEnemies = enemies.Where(e => e.CurrentHP > 0).ToList();

        if (player.CurrentHP <= 0 && aliveEnemies.Count > 0) { onLog?.Invoke("Game Over"); onGameOver?.Invoke(); return true; }
        if (aliveEnemies.Count == 0 && player.CurrentHP > 0) { onLog?.Invoke("Game Win!"); onGameWin?.Invoke(); return true; }
        if (player.CurrentHP <= 0 && aliveEnemies.Count == 0) { onLog?.Invoke("Draw! (both defeated)"); return true; }
        return false;
    }

    void SwapCtxEnemy(SimpleCombatant newEnemy)
    {
        if (ctx == null || newEnemy == null) return;
        ctx.SetEnemy(newEnemy);
    }

    public void SetThreshold(int value)
    {
        ctx.Threshold = Mathf.Max(5, value);
        ctx.OnLog?.Invoke($"[Rule] Threshold → {ctx.Threshold}");
        foreach (var kv in ctx.Phases)
            ctx.OnProgress?.Invoke(kv.Key.Item1, kv.Key.Item2, kv.Value.Total, ctx.Threshold);
    }
    [SerializeField, Min(0f)] float animImpactTimeout = 2.0f; // opsiyonel güvenlik
    [SerializeField, Min(0f)] float animDoneTimeout   = 2.0f;

    IEnumerator PlayAttackSequence(SimpleCombatant attacker, SimpleCombatant defender, int damage, System.Action onImpact = null)
    {
        // 1) Önce bayrakları temizle
        _animImpact = false;
        _animDone   = false;

        // 2) Sonra isteği yayınla (YANLIŞ SIRA buydu)
        onAttackAnimationRequest?.Invoke(attacker, defender, damage);

        // 3) Impact bekle (timeout'lu)
        float t = animImpactTimeout;
        while (!_animImpact && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        // Eğer hiç dinleyici yoksa veya kaçırdıysak, yine de ilerle
        onImpact?.Invoke();

        // 4) Bitışı bekle (timeout'lu)
        t = animDoneTimeout;
        while (!_animDone && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
    }
}
