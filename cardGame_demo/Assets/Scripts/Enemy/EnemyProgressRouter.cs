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

    // Aktif düşman mı?
    bool _isActiveEnemy = false;

    // YENİ: round boyunca kilitlenen (final) değerler
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
            gameDirector.onEnemyPhaseEnded.AddListener(OnEnemyPhaseEnded);   // YENİ
        }
        InitToZero();                // ilk açılışta 0’a çek
        _defLocked = _atkLocked = -1;
    }

    void OnDisable()
    {
        if (gameDirector)
        {
            gameDirector.onProgress.RemoveListener(OnProgress);
            gameDirector.onRoundStarted.RemoveListener(OnRoundStarted);
            gameDirector.onEnemyTurnIndexChanged.RemoveListener(OnEnemyTurnIndexChanged);
            gameDirector.onEnemyPhaseEnded.RemoveListener(OnEnemyPhaseEnded); // YENİ
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

    void InitToZero()
    {
        int max = gameDirector ? gameDirector.GetThresholdSafe() : 21;
        if (defText) defText.SetText(format, 0, max);
        if (atkText) atkText.SetText(format, 0, max);
    }

    void OnRoundStarted()
    {
        _isActiveEnemy = false;
        _defLocked = _atkLocked = -1; // YENİ: kilitleri resetle
        InitToZero();
    }

    void OnEnemyTurnIndexChanged(SimpleCombatant currentEnemy, int index)
    {
        _isActiveEnemy = currentEnemy == self;
    }

    // YENİ: Faz bittiğinde koordinatör "final total" gönderiyor -> kilitle
    void OnEnemyPhaseEnded(SimpleCombatant enemy, PhaseKind phase, int total)
    {
        if (enemy != self) return;

        int max = gameDirector ? gameDirector.GetThresholdSafe() : 21;

        if (phase == PhaseKind.Defense)
        {
            _defLocked = Mathf.Max(0, total);
            if (defText) defText.SetText(format, _defLocked, max);
            if (logWhenUpdated) Debug.Log($"[EnemyProgressRouter:{self?.name}] DEF LOCK = {_defLocked}/{max}");
        }
        else if (phase == PhaseKind.Attack)
        {
            _atkLocked = Mathf.Max(0, total);
            if (atkText) atkText.SetText(format, _atkLocked, max);
            if (logWhenUpdated) Debug.Log($"[EnemyProgressRouter:{self?.name}] ATK LOCK = {_atkLocked}/{max}");
        }
    }

    void OnProgress(Actor actor, PhaseKind phase, int current, int max)
    {
        if (actor != Actor.Enemy) return;
        if (!_isActiveEnemy) return;

        // Faz kilitlendiyse ara güncellemeleri yok say
        if (phase == PhaseKind.Defense)
        {
            if (_defLocked >= 0) return; // artık yazma
            if (defText) defText.SetText(format, current, max);
        }
        else if (phase == PhaseKind.Attack)
        {
            if (_atkLocked >= 0) return;
            if (atkText) atkText.SetText(format, current, max);
        }

        if (logWhenUpdated)
            Debug.Log($"[EnemyProgressRouter:{self?.name}] {phase} => {current}/{max}");
    }
}
