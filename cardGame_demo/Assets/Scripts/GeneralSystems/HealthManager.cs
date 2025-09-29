using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class HealthManager : MonoBehaviour
{
    [Header("Base")]
    [SerializeField, Min(1)] private int maxHP = 80;

    [Header("Runtime")]
    [SerializeField] private int currentHP;
    [SerializeField] private int currentBlock;

    // === Events ===
    [System.Serializable] public class IntEvent : UnityEvent<int> {}
    [System.Serializable] public class IntIntEvent : UnityEvent<int,int> {}

    // Değişimler
    public IntEvent    OnMaxHPChanged  = new IntEvent();       // newMax
    public IntEvent    OnHPChanged     = new IntEvent();       // newHP
    public IntEvent    OnBlockChanged  = new IntEvent();       // newBlock

    // Olaylar
    public IntIntEvent OnDamaged       = new IntIntEvent();    // damageApplied, damageBlocked
    public IntEvent    OnHealed        = new IntEvent();       // healAmount
    public UnityEvent  OnDeath         = new UnityEvent();

    // === Props (read-only dışarı) ===
    public int MaxHP       => maxHP;
    public int CurrentHP   => currentHP;
    public int CurrentBlock=> currentBlock;

    void Awake()
    {
        // Başlangıç doldurma (currentHP 0 ise tam dolu başlat)
        if (currentHP <= 0) currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (currentHP == 0) currentHP = maxHP;
        currentBlock = Mathf.Max(0, currentBlock);
    }

    // === Mutations ===

    public void SetMaxHP(int newMax, bool keepRatio = true)
    {
        newMax = Mathf.Max(1, newMax);
        if (newMax == maxHP) return;

        float ratio = keepRatio && maxHP > 0 ? currentHP / (float)maxHP : 1f;
        maxHP = newMax;
        OnMaxHPChanged?.Invoke(maxHP);

        int newCur = Mathf.Clamp(Mathf.RoundToInt(maxHP * ratio), 0, maxHP);
        if (newCur != currentHP)
        {
            currentHP = newCur;
            OnHPChanged?.Invoke(currentHP);
            if (currentHP <= 0) OnDeath?.Invoke();
        }
    }

    public void SetHP(int newHP)
    {
        newHP = Mathf.Clamp(newHP, 0, maxHP);
        if (newHP == currentHP) return;

        currentHP = newHP;
        OnHPChanged?.Invoke(currentHP);

        if (currentHP <= 0) OnDeath?.Invoke();
    }

    public void RefillToMax()
    {
        if (currentHP != maxHP)
        {
            currentHP = maxHP;
            OnHPChanged?.Invoke(currentHP);
        }
    }

    public void Heal(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        int before = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        int healed = currentHP - before;

        if (healed > 0)
        {
            OnHPChanged?.Invoke(currentHP);
            OnHealed?.Invoke(healed);
        }
    }

    public void GainBlock(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        currentBlock += amount;
        OnBlockChanged?.Invoke(currentBlock);
    }

    public void SetBlock(int value)
    {
        value = Mathf.Max(0, value);
        if (value == currentBlock) return;

        currentBlock = value;
        OnBlockChanged?.Invoke(currentBlock);
    }

    public void ClearBlock()
    {
        if (currentBlock != 0)
        {
            currentBlock = 0;
            OnBlockChanged?.Invoke(currentBlock);
        }
    }

    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        int left = amount;
        int blocked = 0;

        if (currentBlock > 0)
        {
            blocked = Mathf.Min(currentBlock, left);
            currentBlock -= blocked;
            left -= blocked;
            OnBlockChanged?.Invoke(currentBlock);
        }

        int applied = 0;
        if (left > 0)
        {
            int before = currentHP;
            currentHP = Mathf.Max(0, currentHP - left);
            applied = before - currentHP;
            OnHPChanged?.Invoke(currentHP);

            if (currentHP <= 0)
                OnDeath?.Invoke();
        }

        OnDamaged?.Invoke(applied, blocked);
    }
}
