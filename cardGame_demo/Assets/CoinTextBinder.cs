using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class CoinTextBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI text; // Boş bırakılırsa aynı objede aranır

    [Header("Format")]
    [SerializeField] private string prefix = "";                // Örn: "Coins: "
    [SerializeField] private string suffix = "";                // Örn: " c"
    [SerializeField] private bool useThousandsSeparator = true; // 1,234
    [SerializeField] private string customNumericFormat = "";   // Örn: "N0" (öncelikli)

    [Header("Init")]
    [SerializeField] private bool autoRetryUntilFound = true;   // Instance geç gelirse tekrar dene
    [SerializeField, Min(0.05f)] private float retryInterval = 0.2f;

    private bool _subscribed;

    // Kolay erişim
    private PlayerWallet Wallet => PlayerWallet.Instance;

    void Reset()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Awake()
    {
        if (!text) text = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        TrySubscribeWallet();
        RefreshNow();

        if (autoRetryUntilFound && !_subscribed)
            InvokeRepeating(nameof(TrySubscribeWallet), retryInterval, retryInterval);
    }

    void OnDisable()
    {
        UnsubscribeWallet();
        CancelInvoke(nameof(TrySubscribeWallet));
    }

    void TrySubscribeWallet()
    {
        if (_subscribed) return;
        if (Wallet == null) return;

        Debug.Log($"[CoinTextBinder] Subscribed to wallet '{Wallet.name}' (id={Wallet.GetInstanceID()})");
        Wallet.onCoinsChanged.AddListener(OnCoinsChanged);
        _subscribed = true;
        RefreshNow();
        CancelInvoke(nameof(TrySubscribeWallet));
    }


    void UnsubscribeWallet()
    {
        if (!_subscribed) return;
        if (Wallet != null)
            Wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
        _subscribed = false;
    }

    void OnCoinsChanged(int newValue) => SetText(newValue);

    public void RefreshNow()
    {
        SetText(Wallet != null ? Wallet.GetCoins() : 0);
    }

    void SetText(int value)
    {
        if (!text) return;

        string numberPart;
        if (!string.IsNullOrEmpty(customNumericFormat))
            numberPart = value.ToString(customNumericFormat);
        else if (useThousandsSeparator)
            numberPart = value.ToString("N0");
        else
            numberPart = value.ToString();

        text.text = $"{prefix}{numberPart}{suffix}";
    }

    // Dışarıdan elle güncellemek istersen
    public void ForceRefresh() => RefreshNow();
}
