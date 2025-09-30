using UnityEngine;
using UnityEngine.Events;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }
    [System.Serializable] public class IntEvent : UnityEngine.Events.UnityEvent<int> {}

    [SerializeField, Min(0)] private int coins;
    public IntEvent onCoinsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Oyuna girildiğinde UI senkron olsun
        onCoinsChanged?.Invoke(coins);
    }

#if UNITY_EDITOR
    // Play modda Inspector’dan coin değiştirince UI’ı güncelle
    void OnValidate()
    {
        if (Application.isPlaying)
            onCoinsChanged?.Invoke(coins);
    }
#endif
    public void InitFromPlayerData(PlayerData data)
    {
        SetCoins(data != null ? data.startingCoins : 0);
    }
    public int GetCoins() => coins;

    public void SetCoins(int value)
    {
        value = Mathf.Max(0, value);
        if (value == coins) return;
        coins = value;
        onCoinsChanged?.Invoke(coins);
    }

    public void AddCoins(int delta) => SetCoins(coins + Mathf.Max(0, delta));

    public bool SpendCoins(int cost)
    {
        if (cost < 0 || coins < cost) return false;
        SetCoins(coins - cost);
        return true;
    }

    // Inspector’dan sağ tık => hızlı test
    [ContextMenu("Add 10 Coins")]  void CtxAdd10()  => AddCoins(10);
    [ContextMenu("Set 123 Coins")] void CtxSet123() => SetCoins(123);
}
