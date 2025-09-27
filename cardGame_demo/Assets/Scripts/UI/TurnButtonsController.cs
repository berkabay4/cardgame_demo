using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TurnButtonsController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button drawButton;
    [SerializeField] private Button acceptButton;

    [Header("Optional")]
    [Tooltip("Boş bırakıldıysa sahnede otomatik bulmaya çalışır.")]
    [SerializeField] private ActionCoordinator coordinator;

    [Tooltip("Start → StartGame, Draw → OnDrawClicked, Accept → OnAcceptClicked bağla.")]
    [SerializeField] private bool autoWireOnClick = true;

    private bool gameStarted = false;

    void Reset()
    {
        // Inspector’da kolay doldurma
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
        if (!startButton)  startButton  = transform.Find("Start") ?.GetComponent<Button>();
        if (!drawButton)   drawButton   = transform.Find("Draw")  ?.GetComponent<Button>();
        if (!acceptButton) acceptButton = transform.Find("Accept")?.GetComponent<Button>();
    }

    void Awake()
    {
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);

        // İlk durumda: oyun başlamadı → sadece Start açık
        SetBeforeStartUI();

        if (autoWireOnClick)
            WireClicks();
    }

    void OnEnable()
    {
        if (!coordinator) coordinator = FindFirstObjectByType<ActionCoordinator>(FindObjectsInactive.Include);
        if (!coordinator) return;

        coordinator.onGameStarted.AddListener(OnGameStarted);
        coordinator.onRoundStarted.AddListener(OnRoundStarted);
    }

    void OnDisable()
    {
        if (!coordinator) return;
        coordinator.onGameStarted.RemoveListener(OnGameStarted);
        coordinator.onRoundStarted.RemoveListener(OnRoundStarted);
    }

    void WireClicks()
    {
        // Emniyetli temizleyip yeniden bağla
        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                // UI hemen güncellensin
                SetInTurnUI();
                // Koordinatöre başlat de
                coordinator?.StartGame();
            });
        }

        if (drawButton)
        {
            drawButton.onClick.RemoveAllListeners();
            drawButton.onClick.AddListener(() => coordinator?.OnDrawClicked());
        }

        if (acceptButton)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => coordinator?.OnAcceptClicked());
        }
    }

    // --- Coordinator Events ---
    void OnGameStarted()
    {
        gameStarted = true;
        SetInTurnUI();
    }

    void OnRoundStarted()
    {
        // Her el başında da aynı kural geçerli
        if (gameStarted) SetInTurnUI();
    }

    // --- UI States ---
    void SetBeforeStartUI()
    {
        gameStarted = false;
        SetActive(startButton,  true);
        SetActive(drawButton,   false);
        SetActive(acceptButton, false);
    }

    void SetInTurnUI()
    {
        SetActive(startButton,  false);
        SetActive(drawButton,   true);
        SetActive(acceptButton, true);
    }

    static void SetActive(Button btn, bool active)
    {
        if (!btn) return;
        btn.gameObject.SetActive(active);
        btn.interactable = active; // görünürlük dışında da güvence olsun
    }
}
