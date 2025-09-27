using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class EnemyClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] SimpleCombatant self;
    [SerializeField] ActionCoordinator coordinator;

    void Reset()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!self || !coordinator) return;
        coordinator.SelectTarget(self);
    }
}
