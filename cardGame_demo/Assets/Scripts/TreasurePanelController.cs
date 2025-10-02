// TreasurePanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class TreasurePanelController : MonoBehaviour
{
    [Header("Root/UI")]
    [SerializeField] GameObject root;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] Button acceptButton;
    [SerializeField] Button refuseButton;
    [SerializeField] Button mapButton; // başlangıçta gizli

    [Header("Events")]
    public UnityEvent onAccepted;      // ödül verildikten sonra
    public UnityEvent onRefused;       // reddedildiğinde
    public UnityEvent onMapRequested;  // Map butonuna basılınca

    // Runtime
    System.Action _applyReward; // Accept’te çalışacak (coin ekle veya relic ver)
    bool _resolved;

    void Awake()
    {
        if (!root) root = gameObject;

        if (acceptButton) acceptButton.onClick.AddListener(Accept);
        if (refuseButton) refuseButton.onClick.AddListener(Refuse);
        if (mapButton)    mapButton.onClick.AddListener(GoMap);

        root.SetActive(false);
        if (mapButton) mapButton.gameObject.SetActive(false);
    }

    public void Open_Coin(int coins, System.Action applyReward)
    {
        _applyReward = applyReward;
        _resolved = false;

        if (titleText) titleText.text = "Treasure (Coins)";
        if (descText)  descText.text  = $"Bulduğun hazine: <b>+{coins}</b> coin";

        // butonlar
        if (acceptButton) { acceptButton.gameObject.SetActive(true); acceptButton.interactable = true; }
        if (refuseButton) { refuseButton.gameObject.SetActive(true); refuseButton.interactable = true; }
        if (mapButton)    mapButton.gameObject.SetActive(false);

        root.SetActive(true);
    }

    public void Open_Relic(int relicId, string relicName, System.Action applyReward)
    {
        _applyReward = applyReward;
        _resolved = false;

        if (titleText) titleText.text = "Treasure (Relic)";
        if (descText)  descText.text  = $"Bulduğun relic: <b>{(string.IsNullOrEmpty(relicName) ? $"Relic #{relicId}" : relicName)}</b>";

        if (acceptButton) { acceptButton.gameObject.SetActive(true); acceptButton.interactable = true; }
        if (refuseButton) { refuseButton.gameObject.SetActive(true); refuseButton.interactable = true; }
        if (mapButton)    mapButton.gameObject.SetActive(false);

        root.SetActive(true);
    }

    void Accept()
    {
        if (_resolved) return;
        _resolved = true;

        _applyReward?.Invoke();
        onAccepted?.Invoke();

        // Accept/Refuse gizle → Map aç
        if (acceptButton) acceptButton.gameObject.SetActive(false);
        if (refuseButton) refuseButton.gameObject.SetActive(false);
        if (mapButton)    mapButton.gameObject.SetActive(true);
    }

    void Refuse()
    {
        if (_resolved) return;
        _resolved = true;

        onRefused?.Invoke();

        if (acceptButton) acceptButton.gameObject.SetActive(false);
        if (refuseButton) refuseButton.gameObject.SetActive(false);
        if (mapButton)    mapButton.gameObject.SetActive(true);
    }

    void GoMap()
    {
        onMapRequested?.Invoke();
        Close();
    }

    public void Close()
    {
        if (root) root.SetActive(false);
    }
}
