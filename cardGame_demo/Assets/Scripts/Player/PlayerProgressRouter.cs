using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProgressRouter : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] ActionCoordinator coordinator;
    [SerializeField] TextMeshProUGUI defText; // Canvas/child[0]
    [SerializeField] TextMeshProUGUI atkText; // Canvas/child[1]

    [Header("Format")]
    [SerializeField] string format = "{0} / {1}";
    [SerializeField] bool logWhenUpdated = false;

    void Reset()
    {
        AutoWire();
    }

    void Awake()
    {
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
        }
        InitToZero();
    }

    void OnDisable()
    {
        if (coordinator)
        {
            coordinator.onProgress.RemoveListener(OnProgress);
            coordinator.onRoundStarted.RemoveListener(OnRoundStarted);
        }
    }

    void AutoWire()
    {
        // Bu script Player (SimpleCombatant) objesine konulsun
        // Altındaki "Canvas" child'ını bul, onun 0 ve 1. çocuklarından TMP al
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
        if (defText) defText.SetText(format, 0, coordinator ? coordinator.GetThresholdSafe() : 21);
        if (atkText) atkText.SetText(format, 0, coordinator ? coordinator.GetThresholdSafe() : 21);
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
