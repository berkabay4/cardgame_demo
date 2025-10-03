using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProgressRouter : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] CombatDirector combatDirector;
    [SerializeField] TextMeshProUGUI defText; // Canvas/child[0]
    [SerializeField] TextMeshProUGUI atkText; // Canvas/child[1]

    [Header("Format")]
    [SerializeField] string format = "{0} / {1}";
    [SerializeField] bool logWhenUpdated = false;

    void Awake()
    {
        AutoWire();
    }

    void OnEnable()
    {
        // CombatDirector hazır değilse, ContextReady eventini dinle
        if (CombatDirector.Instance == null)
        {
            CombatDirector.ContextReady += OnContextReady;
        }
        else
        {
            HookToDirector(CombatDirector.Instance);
        }

        InitToZero();
    }

    void OnDisable()
    {
        if (combatDirector)
        {
            combatDirector.onProgress.RemoveListener(OnProgress);
            combatDirector.onRoundStarted.RemoveListener(OnRoundStarted);
        }
        CombatDirector.ContextReady -= OnContextReady;
    }

    void OnContextReady()
    {
        if (CombatDirector.Instance != null)
            HookToDirector(CombatDirector.Instance);
        InitToZero();
    }

    void HookToDirector(CombatDirector dir)
    {
        combatDirector = dir;
        combatDirector.onProgress.AddListener(OnProgress);
        combatDirector.onRoundStarted.AddListener(OnRoundStarted);
    }

    void AutoWire()
    {
        // UI referanslarını bağla
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
    }

    void InitToZero()
    {
        var ctx = (combatDirector != null) ? combatDirector.Ctx : null;

        int defMax = (ctx != null) ? ctx.GetThreshold(Actor.Player, PhaseKind.Defense) : 21;
        int atkMax = (ctx != null) ? ctx.GetThreshold(Actor.Player, PhaseKind.Attack)  : 21;

        if (defText) defText.SetText(format, 0, defMax);
        if (atkText) atkText.SetText(format, 0, atkMax);
    }

    void OnRoundStarted()
    {
        InitToZero();
    }

    void OnProgress(Actor actor, PhaseKind phase, int current, int max)
    {
        if (actor != Actor.Player) return;

        if (phase == PhaseKind.Defense && defText)
            defText.SetText(format, current, max);
        else if (phase == PhaseKind.Attack && atkText)
            atkText.SetText(format, current, max);

        if (logWhenUpdated)
            Debug.Log($"[PlayerProgressRouter] {phase} => {current}/{max}");
    }
}
