using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class EnemyClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] SimpleCombatant self;
    [SerializeField] GameDirector coordinator;

    void Awake()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
    }

    void Start()
    {
        coordinator ??= GameDirector.Instance 
                     ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!self || !coordinator || coordinator.State == null) return;

        // BattleState üzerinden güvenli okuma
        var state = coordinator.State;

        Debug.Log($"[EnemyClickTarget] Click on {name}. waiting={state.WaitingForTarget} step={state.Step}");

        // Sadece SelectTarget adımında ve hedef beklenirken tıklamayı işle
        if (state.Step != TurnStep.SelectTarget || !state.WaitingForTarget)
            return;

        coordinator.SelectTarget(self);
    }
}
