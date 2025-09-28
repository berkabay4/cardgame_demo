using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProgressRouter : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] GameDirector gameDirector;
    [SerializeField] SimpleCombatant self;
    [SerializeField] TextMeshProUGUI defText; // Canvas/child[0]
    [SerializeField] TextMeshProUGUI atkText; // Canvas/child[1]

    [Header("Format")]
    [SerializeField] string format = "{0} / {1}";
    [SerializeField] bool logWhenUpdated = false;

    bool _isActiveEnemy = false;

    // round boyunca faz kilitlenince sabitlenen değerler
    int _defLocked = -1;
    int _atkLocked = -1;

    void Reset() => AutoWire();

    void Awake()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
        if (!gameDirector) gameDirector = FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
        if (!defText || !atkText) AutoWire();
    }

    void OnEnable()
    {
        if (!gameDirector) gameDirector = FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
        if (gameDirector)
        {
            gameDirector.onProgress.AddListener(OnProgress);
            gameDirector.onRoundStarted.AddListener(OnRoundStarted);
            gameDirector.onEnemyTurnIndexChanged.AddListener(OnEnemyTurnIndexChanged);
            gameDirector.onEnemyPhaseEnded.AddListener(OnEnemyPhaseEnded);
        }
        _defLocked = _atkLocked = -1;
        InitToZeroForSelf();
    }

    void OnDisable()
    {
        if (gameDirector)
        {
            gameDirector.onProgress.RemoveListener(OnProgress);
            gameDirector.onRoundStarted.RemoveListener(OnRoundStarted);
            gameDirector.onEnemyTurnIndexChanged.RemoveListener(OnEnemyTurnIndexChanged);
            gameDirector.onEnemyPhaseEnded.RemoveListener(OnEnemyPhaseEnded);
        }
    }

    void AutoWire()
    {
        if (!self) self = GetComponent<SimpleCombatant>();

        var canvas = transform.GetComponentInChildren<Canvas>(true)?.transform;
        if (!canvas) canvas = transform.Find("Canvas");

        if (canvas && canvas.childCount >= 2)
        {
            if (!defText) defText = canvas.GetChild(0).GetComponentInChildren<TextMeshProUGUI>(true);
            if (!atkText) atkText = canvas.GetChild(1).GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (!gameDirector) gameDirector = FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
    }

    // --- helpers ---

int GetPhaseMax(PhaseKind phase)
{
    // 1) Kendi EnemyData'sı varsa ondan oku
    var provider = self ? self.GetComponent<EnemyTargetRangeProvider>() : null;
    if (provider && provider.enemyData)
    {
        return phase == PhaseKind.Attack
            ? Mathf.Max(5, provider.enemyData.maxAttackRange)
            : Mathf.Max(5, provider.enemyData.maxdefenceRange);
    }

    // 2) Context per-phase threshold (yalnızca bu düşman Context’te aktifse)
    var ctx = gameDirector ? gameDirector.Ctx : null;
    if (ctx != null && ctx.Enemy == self)
        return ctx.GetThreshold(Actor.Enemy, phase);

    // 3) Fallback
    return gameDirector ? gameDirector.GetThresholdSafe() : 21;
}

void InitToZeroForSelf()
{
    if (_defLocked < 0 && defText) defText.SetText(format, 0, GetPhaseMax(PhaseKind.Defense));
    if (_atkLocked < 0 && atkText) atkText.SetText(format, 0, GetPhaseMax(PhaseKind.Attack));
}

void OnEnemyPhaseEnded(SimpleCombatant enemy, PhaseKind phase, int total)
{
    if (enemy != self) return;
    int max = GetPhaseMax(phase);            // ← kendi max
    if (phase == PhaseKind.Defense)
    {
        _defLocked = Mathf.Max(0, total);
        if (defText) defText.SetText(format, _defLocked, max);
    }
    else
    {
        _atkLocked = Mathf.Max(0, total);
        if (atkText) atkText.SetText(format, _atkLocked, max);
    }
}

    // --- event handlers ---

    void OnRoundStarted()
    {
        _isActiveEnemy = false;
        _defLocked = _atkLocked = -1;
        InitToZeroForSelf();
    }

    void OnEnemyTurnIndexChanged(SimpleCombatant currentEnemy, int index)
    {
        _isActiveEnemy = (currentEnemy == self);

        // Bu düşman şimdi aktif olduysa, doğru max’larla 0/Max göster (kilitlenmemişse)
        if (_isActiveEnemy)
            InitToZeroForSelf();
    }

    void OnProgress(Actor actor, PhaseKind phase, int current, int max)
    {
        if (actor != Actor.Enemy) return;

        // En sağlam filtre: Context’teki aktif enemy bu mu?
        var ctxEnemy = gameDirector && gameDirector.Ctx != null ? gameDirector.Ctx.Enemy : null;
        if (ctxEnemy != self) return;   // ← pasif düşmanların HUD’u hiç etkilenmesin

        // Faz kilitliyse yazma
        if (phase == PhaseKind.Defense)
        {
            if (_defLocked >= 0) return;
            if (defText) defText.SetText(format, current, max);
        }
        else
        {
            if (_atkLocked >= 0) return;
            if (atkText) atkText.SetText(format, current, max);
        }

        if (logWhenUpdated)
            Debug.Log($"[EnemyProgressRouter:{self?.name}] {phase} => {current}/{max}");
    }

}
