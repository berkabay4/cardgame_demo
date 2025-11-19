using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Elite Enemy/ Mini Boss Definition", fileName = "MiniBossDefinition")]
public class MiniBossDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName = "Mini Boss";

    [Header("Visuals")]
    public Sprite sprite;          // UI / kart görseli
    public GameObject prefab;        // Sahnede spawn edilecek prefab

    [Header("Stats")]
    public int maxHealth = 100;
    [Tooltip("Bu miniboss’un basic saldırı damage aralığı")]
    public IntRange attackDamageRange = new IntRange(5, 15);

    [Header("Attack Behaviour")]
    [Tooltip("Bu miniboss’un turunda hangi saldırı patternini kullanacağını belirleyen ScriptableObject.")]
    public MiniBossAttackBehaviour attackBehaviour;
}
