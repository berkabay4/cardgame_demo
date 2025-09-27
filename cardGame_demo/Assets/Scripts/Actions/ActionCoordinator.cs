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

public class ActionCoordinator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(1)] int threshold = 21;
    [SerializeField] bool reshuffleWhenLow = true;
    [SerializeField, Min(5)] int lowDeckCount = 8;
    [SerializeField] Vector2 enemyDrawDelayRange = new Vector2(0.5f, 1.5f);
    public int GetThresholdSafe() => ctx != null ? ctx.Threshold : threshold;
    [Header("Refs")]
    [SerializeField] SimpleCombatant player;
    [SerializeField] List<SimpleCombatant> enemies = new List<SimpleCombatant>(); // çoklu düşman

    [Header("UnityEvents (Bridge/UI)")]
    public UnityEvent<Actor,PhaseKind,int,int> onProgress;
    public UnityEvent<Actor,PhaseKind,Card> onCardDrawn;
    public UnityEvent<string> onLog;
    public UnityEvent onGameOver;
    public UnityEvent onGameWin;

    [Header("State Events")]
    public StepEvent onStepChanged;                         // PlayerDef, PlayerAtk, EnemyDef, EnemyAtk, Resolve
    public BoolEvent onWaitingForTargetChanged;             // hedef seçimi bekleniyor mu
    public EnemyIdxEvent onEnemyTurnIndexChanged;           // sıradaki düşman (enemy, index)
    public EnemyPhaseStartedEvent onEnemyPhaseStarted;      // düşman fazı başladı
    public EnemyPhaseEndedEvent   onEnemyPhaseEnded;        // düşman fazı bitti (toplam)
    public UnityEvent onRoundStarted;                       // yeni el başladı
    public UnityEvent onRoundResolved;                      // resolve bitti
    public IntEvent onPlayerDefLocked;                      // player DEF accept sonrası total
    public IntEvent onPlayerAtkLocked;                      // player ATK accept sonrası total
    public TargetEvent onTargetChanged;                     // hedef değişti

    // İç durum
    CombatContext ctx;              // tek deste; tüm round için
    ActionQueue queue;
    TurnStep step;

    bool isBusy;
    bool waitingForTarget;          // Player ATK accept sonrası hedef seçimi beklenir
    SimpleCombatant currentTarget;  // oyuncunun saldıracağı düşman

    // Düşman başına DEF/ATK snapshot
    readonly Dictionary<SimpleCombatant, int> enemyDefTotals = new();
    readonly Dictionary<SimpleCombatant, int> enemyAtkTotals = new();

    int playerDefTotal; // round boyunca tek
    int playerAtkTotal; // seçilen hedefe uygulanır

    void Awake()
    {
        // Enemies boşsa sahneden auto topla (player hariç)
        if (enemies == null || enemies.Count == 0)
        {
            var all = FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            enemies = all.Where(e => e != player).ToList();
        }

        var firstEnemy = enemies.Count > 0 ? enemies[0] : null;
        ctx = new CombatContext(threshold, new DeckService(), player, firstEnemy);

        // Köprüler
        ctx.OnProgress.AddListener((a,k,cur,max)=> onProgress?.Invoke(a,k,cur,max));
        ctx.OnCardDrawn.AddListener((a,k,c)=> onCardDrawn?.Invoke(a,k,c));
        ctx.OnLog.AddListener(s=> onLog?.Invoke(s));

        queue = new ActionQueue();

        StartNewTurn();
    }
        // === Helpers (tek yerden event) ===
    void SetStep(TurnStep s)
    {
        step = s;
        onStepChanged?.Invoke(step);
        AnnounceStep();
    }

    void SetWaitingForTarget(bool v)
    {
        if (waitingForTarget == v) { waitingForTarget = v; return; }
        waitingForTarget = v;
        onWaitingForTargetChanged?.Invoke(waitingForTarget);
    }

    void StartNewTurn()
    {
        onRoundStarted?.Invoke();

        // Round state temizliği
        SetWaitingForTarget(false);
        currentTarget = null;
        playerDefTotal = 0;
        playerAtkTotal = 0;
        enemyDefTotals.Clear();
        enemyAtkTotals.Clear();

        queue.Enqueue(new StartTurnAction(reshuffleWhenLow, lowDeckCount));
        StartCoroutine(queue.RunAllCoroutine(ctx));

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

            var acc = ctx.GetAcc(Actor.Player, PhaseKind.Defense);
            if (acc.IsBusted) GoNextFromPlayerDef();
        }
        else if (step == TurnStep.PlayerAtk)
        {
            queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack));
            StartCoroutine(queue.RunAllCoroutine(ctx));

            var acc = ctx.GetAcc(Actor.Player, PhaseKind.Attack);
            if (acc.IsBusted) GoNextFromPlayerAtk();
        }
        // Enemy aşamaları coroutine’lerde sırayla işlenecek
    }

    // === UI: Draw / Accept ===
    public void OnDrawClicked()
    {
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

                // Hedef seçimi bekle
                SetWaitingForTarget(true);
                onLog?.Invoke($"[SelectTarget] Your ATK={playerAtkTotal}. Click an enemy to target.");
                break;

            default:
                onLog?.Invoke("Accept is not available in this step.");
                break;
        }
    }

    void GoNextFromPlayerDef()
    {
        playerDefTotal = 0; // bust → 0
        BeginPhase(TurnStep.PlayerAtk);
    }

    void GoNextFromPlayerAtk()
    {
        playerAtkTotal = 0; // bust → 0
        player.CurrentAttack = 0;

        // Hedefe ihtiyaç yok; direkt düşman turlarına geç
        SetWaitingForTarget(false);
        if (enemyTurnCo != null) StopCoroutine(enemyTurnCo);
        enemyTurnCo = StartCoroutine(EnemiesAutoTwoPhasesRoutine());
    }

    Coroutine enemyTurnCo;

    // UI/Click script’inden çağırılacak güvenli seçim
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

        if (enemyTurnCo != null) StopCoroutine(enemyTurnCo);
        enemyTurnCo = StartCoroutine(EnemiesAutoTwoPhasesRoutine());
        return true;
    }

    // Geri uyumluluk için (doğrudan çağıran varsa)
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

        // --- Enemy DEF (sırayla) ---
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

        // --- Enemy ATK (sırayla) ---
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

        // --- Resolve ---
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

    IEnumerator ResolveAllAndCleanup()
    {
        // === Enemies → Player (toplu hasar, PlayerDEF tek seferde kırpar)
        int totalEnemyAtk = 0;
        foreach (var kv in enemyAtkTotals)
            totalEnemyAtk += Mathf.Max(0, kv.Value);

        int incoming = Mathf.Max(0, totalEnemyAtk - Mathf.Max(0, playerDefTotal));

        // === Player → Target (PlayerATK vs target DEF)
        int playerDamage = 0;
        if (currentTarget != null)
        {
            int targetDef = enemyDefTotals.TryGetValue(currentTarget, out var d) ? d : 0;
            playerDamage = Mathf.Max(0, Mathf.Max(0, playerAtkTotal) - Mathf.Max(0, targetDef));
        }

        // Uygula
        if (incoming > 0)
        {
            player.CurrentHP -= incoming;
            onLog?.Invoke($"You take {incoming} damage (Total Enemy ATK {totalEnemyAtk} - Your DEF {playerDefTotal}).");
        }
        else
        {
            onLog?.Invoke($"You blocked all incoming damage! (Total Enemy ATK {totalEnemyAtk} - Your DEF {playerDefTotal}).");
        }

        if (currentTarget != null)
        {
            if (playerDamage > 0)
            {
                currentTarget.CurrentHP -= playerDamage;
                onLog?.Invoke($"You dealt {playerDamage} to {currentTarget.name} (Your ATK {playerAtkTotal} - {currentTarget.name} DEF {enemyDefTotals[currentTarget]}).");
            }
            else
            {
                onLog?.Invoke($"Your attack couldn’t pierce {currentTarget.name}'s defense.");
            }
        }

        // Win/Lose kontrolü
        if (CheckWinLose()) yield break;

        // Yeni el hazırlığı
        player.CurrentAttack = 0;
        foreach (var e in enemies) if (e) e.CurrentAttack = 0;

        StartNewTurn();
        yield return null;
    }

    bool CheckWinLose()
    {
        enemies.RemoveAll(e => e == null);
        var aliveEnemies = enemies.Where(e => e.CurrentHP > 0).ToList();

        if (player.CurrentHP <= 0 && aliveEnemies.Count > 0)
        {
            onLog?.Invoke("Game Over");
            onGameOver?.Invoke();
            return true;
        }
        if (aliveEnemies.Count == 0 && player.CurrentHP > 0)
        {
            onLog?.Invoke("Game Win!");
            onGameWin?.Invoke();
            return true;
        }
        if (player.CurrentHP <= 0 && aliveEnemies.Count == 0)
        {
            onLog?.Invoke("Draw! (both defeated)");
            return true;
        }
        return false;
    }

    // CombatContext içindeki aktif düşmanı değiştirir
    void SwapCtxEnemy(SimpleCombatant newEnemy)
    {
        if (ctx == null || newEnemy == null) return;
        ctx.SetEnemy(newEnemy); // CombatContext’e eklendiğini varsayıyoruz
    }

    public void SetThreshold(int value)
    {
        ctx.Threshold = Mathf.Max(5, value);
        ctx.OnLog?.Invoke($"[Rule] Threshold → {ctx.Threshold}");

        foreach (var kv in ctx.Phases)
            ctx.OnProgress?.Invoke(kv.Key.Item1, kv.Key.Item2, kv.Value.Total, ctx.Threshold);
    }

    // === Opsiyonel: Debug ===
    [ContextMenu("DBG Print State")]
    void DebugPrintState()
    {
        Debug.Log($"[CoordinatorDBG] step={step}, waitingForTarget={waitingForTarget}, isBusy={isBusy}, pDEF={playerDefTotal}, pATK={playerAtkTotal}, target={(currentTarget? currentTarget.name : "null")}");
    }
}
