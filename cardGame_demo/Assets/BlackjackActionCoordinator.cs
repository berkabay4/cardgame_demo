using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum TurnStep { PlayerDef, PlayerAtk, EnemyDef, EnemyAtk, Resolve }

public class BlackjackActionCoordinator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(1)] int threshold = 21;
    [SerializeField] bool reshuffleWhenLow = true;
    [SerializeField, Min(5)] int lowDeckCount = 8;
    [SerializeField] Vector2 enemyDrawDelayRange = new Vector2(0.5f, 1.5f); // bekleme aralığı

    [Header("Refs")]
    [SerializeField] SimpleCombatant player;
    [SerializeField] SimpleCombatant enemy;

    [Header("UnityEvents (UI)")]
    public UnityEvent<Actor,PhaseKind,int,int> onProgress;
    public UnityEvent<Actor,PhaseKind,Card> onCardDrawn;
    public UnityEvent<string> onLog;
    public UnityEvent onGameOver; // player HP 0
    public UnityEvent onGameWin;  // enemy  HP 0

    CombatContext ctx;
    ActionQueue queue;
    TurnStep step;

    bool isBusy;               // enemy oynarken veya işlemler akarken input kilidi
    Coroutine enemyTurnCo;

    void Awake()
    {
        ctx = new CombatContext(threshold, new DeckService(), player, enemy);
        // Köprüler
        ctx.OnProgress.AddListener((a,k,cur,max)=> onProgress?.Invoke(a,k,cur,max));
        ctx.OnCardDrawn.AddListener((a,k,c)=> onCardDrawn?.Invoke(a,k,c));
        ctx.OnLog.AddListener(s=> onLog?.Invoke(s));

        queue = new ActionQueue();

        // ProgressRouter varsa otomatik bağla ve ilk görünümü bas
        var router = FindFirstObjectByType<ProgressRouter>(FindObjectsInactive.Include);
        if (router)
        {
            onProgress.AddListener(router.OnProgress);
            router.InitAll(threshold);
        }

        StartNewTurn();
    }

    void StartNewTurn()
    {
        // Yeni el başlat
        queue.Enqueue(new StartTurnAction(reshuffleWhenLow, lowDeckCount));
        StartCoroutine(queue.RunAllCoroutine(ctx));

        // Player DEF fazıyla başla (ilk kartı otomatik çeker)
        BeginPhase(TurnStep.PlayerDef);
    }

    void AnnounceStep()
    {
        onLog?.Invoke(step switch
        {
            TurnStep.PlayerDef => "Your Defense: Draw or Accept.",
            TurnStep.PlayerAtk => "Your Attack: Draw or Accept.",
            TurnStep.EnemyDef  => "Enemy choosing Defense...",
            TurnStep.EnemyAtk  => "Enemy choosing Attack...",
            TurnStep.Resolve   => "Resolving...",
            _ => ""
        });
    }

    // Faz başlatıcı: step set + log + (Player fazlarında) otomatik ilk kart
    void BeginPhase(TurnStep newStep)
    {
        step = newStep;
        AnnounceStep();

        if (step == TurnStep.PlayerDef)
        {
            queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Defense)); // OTOMATİK 1. KART
            StartCoroutine(queue.RunAllCoroutine(ctx));

            if (ctx.GetAcc(Actor.Player, PhaseKind.Defense).IsBusted)
                GoNextFromPlayerDef();
        }
        else if (step == TurnStep.PlayerAtk)
        {
            queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack));  // OTOMATİK 1. KART
            StartCoroutine(queue.RunAllCoroutine(ctx));

            if (ctx.GetAcc(Actor.Player, PhaseKind.Attack).IsBusted)
                GoNextFromPlayerAtk();
        }
        // EnemyDef/EnemyAtk'ta AI kendi kartını çeker (coroutine ile aşağıda).
    }

    // === UI: Draw / Accept ===
    public void OnDrawClicked()
    {
        if (isBusy) { onLog?.Invoke("Please wait..."); return; }

        switch (step)
        {
            case TurnStep.PlayerDef:
                queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Defense));
                StartCoroutine(queue.RunAllCoroutine(ctx));

                if (ctx.GetAcc(Actor.Player, PhaseKind.Defense).IsBusted)
                    GoNextFromPlayerDef();
                break;

            case TurnStep.PlayerAtk:
                queue.Enqueue(new DrawCardAction(Actor.Player, PhaseKind.Attack));
                StartCoroutine(queue.RunAllCoroutine(ctx));

                if (ctx.GetAcc(Actor.Player, PhaseKind.Attack).IsBusted)
                    GoNextFromPlayerAtk();
                break;

            default:
                onLog?.Invoke("Draw is not available in this step.");
                break;
        }
    }

    public void OnAcceptClicked()
    {
        if (isBusy) { onLog?.Invoke("Please wait..."); return; }

        switch (step)
        {
            case TurnStep.PlayerDef:
                queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Defense));
                StartCoroutine(queue.RunAllCoroutine(ctx));
                BeginPhase(TurnStep.PlayerAtk);
                break;

            case TurnStep.PlayerAtk:
                queue.Enqueue(new StandAction(Actor.Player, PhaseKind.Attack));
                StartCoroutine(queue.RunAllCoroutine(ctx));

                // Player Attack snapshot
                var pAtk = ctx.GetAcc(Actor.Player, PhaseKind.Attack).Total;
                ctx.GetUnit(Actor.Player).CurrentAttack = pAtk;
                Debug.Log($"[Coordinator] Player.CurrentAttack = {pAtk}");

                // Enemy turn'ü gecikmeli coroutine ile oynat
                if (enemyTurnCo != null) StopCoroutine(enemyTurnCo);
                enemyTurnCo = StartCoroutine(EnemyAutoTwoPhasesRoutine());
                break;

            default:
                onLog?.Invoke("Accept is not available in this step.");
                break;
        }
    }

    void GoNextFromPlayerDef()
    {
        // DEF bust ise değer zaten 0; direkt ATK fazına geç ve ilk kartı çek
        BeginPhase(TurnStep.PlayerAtk);
    }

    void GoNextFromPlayerAtk()
    {
        // ATK bust ise oyuncunun saldırısı 0 kabul edilir
        ctx.GetUnit(Actor.Player).CurrentAttack = 0;
        Debug.Log("[Coordinator] Player.CurrentAttack = 0 (bust)");

        // Enemy turn'ü gecikmeli coroutine ile oynat
        if (enemyTurnCo != null) StopCoroutine(enemyTurnCo);
        enemyTurnCo = StartCoroutine(EnemyAutoTwoPhasesRoutine());
    }

    // Enemy iki fazını gecikmeli oynatır
    IEnumerator EnemyAutoTwoPhasesRoutine()
    {
        isBusy = true;

        // Enemy DEF
        step = TurnStep.EnemyDef; AnnounceStep();
        yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Defense));

        // Enemy ATK
        step = TurnStep.EnemyAtk; AnnounceStep();
        yield return StartCoroutine(RunEnemyPhaseWithDelays(PhaseKind.Attack));

        // Inspector snapshot
        enemy.CurrentAttack = ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;

        // Resolve
        step = TurnStep.Resolve; AnnounceStep();
        queue.Enqueue(new ResolveAction());
        yield return StartCoroutine(queue.RunAllCoroutine(ctx));

        // Win/Lose kontrolü
        if (CheckWinLose()) { isBusy = false; yield break; }

        // Yeni el
        player.CurrentAttack = 0;
        enemy.CurrentAttack  = 0;
        StartNewTurn();

        isBusy = false;
    }

    // Tek enemy fazını oynatır; Draw sonrası rastgele bekler
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

    bool CheckWinLose()
    {
        if (player.CurrentHP <= 0 && enemy.CurrentHP > 0) { onLog?.Invoke("Game Over"); onGameOver?.Invoke(); return true; }
        if (enemy.CurrentHP <= 0 && player.CurrentHP > 0) { onLog?.Invoke("Game Win!"); onGameWin?.Invoke(); return true; }
        if (enemy.CurrentHP <= 0 && player.CurrentHP <= 0){ onLog?.Invoke("Draw! (both defeated)"); return true; }
        return false;
    }

    // İstersen dışarıdan threshold değiştir
    public void SetThreshold(int value)
    {
        ctx.Threshold = Mathf.Max(5, value);
        ctx.OnLog?.Invoke($"[Rule] Threshold → {ctx.Threshold}");
        foreach (var kv in ctx.Phases)
            ctx.OnProgress?.Invoke(kv.Key.Item1, kv.Key.Item2, kv.Value.Total, ctx.Threshold);
    }
}
