using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class EnemyClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] SimpleCombatant self;
    [SerializeField] CombatDirector combatDirector;

    void Awake()
    {
        if (!self) self = GetComponent<SimpleCombatant>();
    }

    void Start()
    {
        combatDirector ??= CombatDirector.Instance 
                     ?? FindFirstObjectByType<CombatDirector>(FindObjectsInactive.Include);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!self || !combatDirector || combatDirector.State == null) return;

        // BattleState üzerinden güvenli okuma
        var state = combatDirector.State;

        Debug.Log($"[EnemyClickTarget] Click on {name}. waiting={state.WaitingForTarget} step={state.Step}");

        // Sadece SelectTarget adımında ve hedef beklenirken tıklamayı işle
        if (state.Step != TurnStep.SelectTarget || !state.WaitingForTarget)
            return;

        combatDirector.SelectTarget(self);
    }
}
