// SimpleCombatant.cs
using UnityEngine;

public class SimpleCombatant : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] public int maxHP = 80;
    public int MaxHP => maxHP;

    [Header("Runtime (Inspector-visible)")]
    [SerializeField] private int currentHP;       // Inspector'da görünür
    [SerializeField] private int currentBlock;    // Inspector'da görünür
    [SerializeField] private int currentAttack;   // Bu eli vuracağı ATK (Accept sonrası set et)

    // Kod tarafından erişim:
    public int CurrentHP    { get => currentHP;    set => currentHP = value; }
    public int Block        { get => currentBlock; set => currentBlock = value; }
    public int CurrentAttack{ get => currentAttack;set => currentAttack = value; }

    private void Awake()
    {
        currentHP = MaxHP;
        currentBlock = 0;
        currentAttack = 0;
    }

    public void GainBlock(int amount)
    {
        int before = currentBlock;
        currentBlock += Mathf.Max(0, amount);
        Debug.Log($"[{name}] BLOCK +{amount} ({before}->{currentBlock})");
    }
    public void ApplyFromStats(PlayerStats stats, bool refillToMax = true)
    {
        if (!stats) return;
        maxHP = Mathf.Max(1, stats.MaxHealth);
        if (refillToMax) CurrentHP = maxHP;
        else CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);
    }
    public void TakeDamage(int amount)
    {
        int left = amount;
        if (currentBlock > 0)
        {
            int absorb = Mathf.Min(currentBlock, left);
            currentBlock -= absorb;
            left -= absorb;
            Debug.Log($"[{name}] Block {absorb} emdi. Kalan Block:{currentBlock}");
        }
        if (left > 0)
        {
            int before = currentHP;
            currentHP = Mathf.Max(0, currentHP - left);
            Debug.Log($"[{name}] {left} hasar. HP {before}->{currentHP}/{MaxHP}");
        }
        else
        {
            Debug.Log($"[{name}] Block tüm hasarı tuttu. HP: {currentHP}/{MaxHP}");
        }
    }
}
