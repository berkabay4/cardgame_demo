using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProgressRouter : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] ActionCoordinator coordinator;
    [SerializeField] SimpleCombatant self;               // Bu düşman
    [SerializeField] TextMeshProUGUI defText;            // Canvas/child[0]
    [SerializeField] TextMeshProUGUI atkText;            // Canvas/child[1]

    [Header("Format")]
    [SerializeField] string format = "{0} / {1}";
    [SerializeField] bool logWhenUpdated = false;

    // Aktif düşman mı? (Koordinatör bildirir)
    bool _isActiveEnemy = false;

    void Reset()
    {
        AutoWire();
    }

    void Awake()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
        if (!defText || !atkText) AutoWire();
    }

    void OnEnable()
    {
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
        if (coordinator)
        {
            coordinator.onProgress.AddListener(OnProgress);
            coordinator.onRoundStarted.AddListener(OnRoundStarted);
            coordinator.onEnemyTurnIndexChanged.AddListener(OnEnemyTurnIndexChanged);
        }
        InitToZero();
    }

    void OnDisable()
    {
        if (coordinator)
        {
            coordinator.onProgress.RemoveListener(OnProgress);
            coordinator.onRoundStarted.RemoveListener(OnRoundStarted);
            coordinator.onEnemyTurnIndexChanged.RemoveListener(OnEnemyTurnIndexChanged);
        }
    }

    void AutoWire()
    {
        if (!self) self = GetComponent<SimpleCombatant>();

        var canvas = transform.GetComponentInChildren<Canvas>(true)?.transform;
        if (!canvas)
        {
            var t = transform.Find("Canvas");
            canvas = t;
        }

        if (canvas && canvas.childCount >= 2)
        {
            if (!defText) defText = canvas.GetChild(0).GetComponentInChildren<TextMeshProUGUI>(true);
            if (!atkText) atkText = canvas.GetChild(1).GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
    }

    void InitToZero()
    {
        int max = coordinator ? coordinator.GetThresholdSafe() : 21;
        if (defText) defText.SetText(format, 0, max);
        if (atkText) atkText.SetText(format, 0, max);
    }

    void OnRoundStarted()
    {
        _isActiveEnemy = false; // round başında kimse aktif sayılmasın
        InitToZero();
    }

    // Koordinatör sırayı bu event ile bildiriyor
    void OnEnemyTurnIndexChanged(SimpleCombatant currentEnemy, int index)
    {
        _isActiveEnemy = currentEnemy == self;
        // İstersen aktif olmayanları hafif saydam yap:
        // var c = defText?.color; if (c.HasValue) { c.Value.a = _isActiveEnemy ? 1f : 0.5f; defText.color = c.Value; }
        // var c2 = atkText?.color; ...
    }

    void OnProgress(Actor actor, PhaseKind phase, int current, int max)
    {
        if (actor != Actor.Enemy) return;
        if (!_isActiveEnemy) return; // sadece aktif düşman kendini günceller

        if (phase == PhaseKind.Defense && defText)
            defText.SetText(format, current, max);
        else if (phase == PhaseKind.Attack && atkText)
            atkText.SetText(format, current, max);

        if (logWhenUpdated)
            Debug.Log($"[EnemyProgressRouter:{self?.name}] {phase} => {current}/{max}");
    }
}
