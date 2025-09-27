using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class EnemyClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] SimpleCombatant self;
    [SerializeField] ActionCoordinator coordinator;

    void Start()
    {
        coordinator ??= ActionCoordinator.Instance 
                     ?? FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
        if (!self) self = GetComponent<SimpleCombatant>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!self || !coordinator) return;

        Debug.Log($"[EnemyClickTarget] Click on {name}. waiting={coordinatorWaiting} step={coordinatorStep}");
        var ok = coordinator.SelectTargetSafe(self);
        if (!ok) Debug.Log("[EnemyClickTarget] SelectTargetSafe ignored (not waiting or invalid target).");
    }

    // küçük yardımcılar (sadece debug için)
    bool coordinatorWaiting => coordinator && 
        (bool)coordinator.GetType().GetField("waitingForTarget", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(coordinator);

    TurnStep coordinatorStep => coordinator ? 
        (TurnStep)coordinator.GetType().GetField("step", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(coordinator)
        : default;
}
