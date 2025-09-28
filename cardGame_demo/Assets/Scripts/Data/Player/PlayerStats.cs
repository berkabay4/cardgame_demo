using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Stats (Runtime)")]
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField, Min(0)] private int currentHealth = 20;
    [SerializeField, Min(5)] private int maxRange = 21; // Blackjack üst sınırı

    [Header("Events")]
    public UnityEvent onInitialized;
    public UnityEvent<int,int> onHealthChanged; // current, max
    public UnityEvent<int> onMaxRangeChanged;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int MaxRange => maxRange;

    public void InitFrom(PlayerData data)
    {
        if (!data) return;
        maxHealth = Mathf.Max(1, data.maxHealth);
        currentHealth = maxHealth;
        maxRange = Mathf.Max(5, data.maxRange);
        onInitialized?.Invoke();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        onMaxRangeChanged?.Invoke(maxRange);
    }

    // Örnek yardımcılar:
    public void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxRange(int value)
    {
        maxRange = Mathf.Max(5, value);
        onMaxRangeChanged?.Invoke(maxRange);
    }
}
