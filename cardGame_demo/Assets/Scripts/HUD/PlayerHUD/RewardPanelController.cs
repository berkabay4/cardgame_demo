using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RewardPanelController : MonoBehaviour
{
    [Header("Panel Root (inactive at start)")]
    [SerializeField] private GameObject panelRoot;                // UI penceresi (kapalı başlayacak)

    [Header("UI Refs (under panelRoot)")]
    [SerializeField] private TextMeshProUGUI baseRewardText;
    [SerializeField] private TextMeshProUGUI finalRewardText;
    [SerializeField] private TextMeshProUGUI progressText;        // "sum/max"
    [SerializeField] private Button drawButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;

    [Header("Settings")]
    [SerializeField] private EconomyConfig economy;               // bustPenalty vs.
    [SerializeField] private bool hideOnAccept = true;

    [Header("54-Card Deck Value Mapping")]
    [Tooltip("As değeri (1 veya 11 tipik).")]
    [SerializeField, Min(0)] private int aceValue = 1;
    [Tooltip("J/Q/K değeri (genelde 10).")]
    [SerializeField, Min(0)] private int faceValue = 10;
    [Tooltip("Joker (2 adet) kart değeri.")]
    [SerializeField, Min(0)] private int jokerValue = 11;

    [Header("Events")]
    public UnityEvent<int> onRewardAccepted; // final coin miktarı

    // --- Runtime ---
    RewardContext ctx;
    readonly List<IRewardRelicEffect> rewardRelics = new();
    System.Random rng = new System.Random();

    List<int> rewardDeckValues;
    int deckIndex;

    void Reset()
    {
        // panelRoot boşsa, bu component'in bağlı olduğu objeyi panel olarak kabul et
        if (!panelRoot) panelRoot = gameObject;

        // Otomatik referans bulma (panelRoot altında)
        if (!baseRewardText)  baseRewardText  = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!finalRewardText) finalRewardText = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!progressText)    progressText    = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!drawButton)      drawButton      = panelRoot.GetComponentInChildren<Button>(true);
        if (!acceptButton)    acceptButton    = panelRoot.GetComponentInChildren<Button>(true);
        if (!cancelButton)    cancelButton    = panelRoot.GetComponentInChildren<Button>(true);
    }

    void Awake()
    {
        if (!panelRoot) panelRoot = gameObject;

        if (drawButton)   drawButton.onClick.AddListener(OnDraw);
        if (acceptButton) acceptButton.onClick.AddListener(OnAccept);
        if (cancelButton) cancelButton.onClick.AddListener(OnCancel);

        // Panel kapalı başlasın
        if (panelRoot.activeSelf) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Event geldiğinde çağır: paneli açar ve mini-oyunu başlatır.
    /// </summary>
    public void Open(int baseReward, PlayerData pData, EconomyConfig econ, IEnumerable<IRewardRelicEffect> relics)
    {
        economy = econ ? econ : economy;

        ctx = new RewardContext
        {
            playerData  = pData,
            economy     = economy,
            baseReward  = baseReward,
            maxRange    = pData ? Mathf.Max(1, pData.maxRewardRange) : 21,
            drawnValues = new List<int>()
        };

        rewardRelics.Clear();
        if (relics != null) rewardRelics.AddRange(relics);

        BuildAndShuffle54Deck();
        UpdateUI();
        panelRoot.SetActive(true);
    }

    // === Deck (54) ===
    void BuildAndShuffle54Deck()
    {
        rewardDeckValues = new List<int>(54);

        // A(4), 2-10 (4'er), J/Q/K (12), Joker (2)
        for (int i = 0; i < 4; i++) rewardDeckValues.Add(aceValue);
        for (int v = 2; v <= 10; v++)
            for (int i = 0; i < 4; i++)
                rewardDeckValues.Add(v);
        for (int i = 0; i < 12; i++) rewardDeckValues.Add(faceValue);
        rewardDeckValues.Add(jokerValue);
        rewardDeckValues.Add(jokerValue);

        // Fisher–Yates
        for (int i = rewardDeckValues.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (rewardDeckValues[i], rewardDeckValues[j]) = (rewardDeckValues[j], rewardDeckValues[i]);
        }
        deckIndex = 0;
    }

    int DrawFromDeck()
    {
        if (rewardDeckValues == null || rewardDeckValues.Count == 0)
            BuildAndShuffle54Deck();

        if (deckIndex >= rewardDeckValues.Count)
            BuildAndShuffle54Deck(); // biterse yeniden karıştır

        return rewardDeckValues[deckIndex++];
    }

    // === UI ===
    void UpdateUI()
    {
        if (baseRewardText) baseRewardText.text = $"Base Reward: {ctx.baseReward}";

        int sum = RewardService.Sum(ctx.drawnValues);
        int max = Mathf.Max(1, ctx.maxRange);
        if (progressText) progressText.text = $"{sum}/{max}";

        int preview = RewardService.ComputeFinal(ctx, rewardRelics);
        if (finalRewardText) finalRewardText.text = $"Final Reward: {preview}";

        bool isBust = ctx.isBust;
        if (drawButton)   drawButton.interactable = !isBust;
        if (acceptButton) acceptButton.interactable = true; // bust olsa da kabul edebilir (cezalı final)
    }

    // === Actions ===
    void OnDraw()
    {
        int v = DrawFromDeck();
        ctx.drawnValues.Add(v);
        UpdateUI();
    }

    void OnAccept()
    {
        int final = RewardService.ComputeFinal(ctx, rewardRelics);

        // SADECE event yayınla — cüzdana EKLEME burada yapılmayacak.
        onRewardAccepted?.Invoke(final);

        if (hideOnAccept) Close();

        // Butonları kilitle (çifte tıklama/çifte event’e karşı güvenlik)
        if (acceptButton) acceptButton.interactable = false;
        if (drawButton)   drawButton.interactable   = false;
    }
    void OnCancel() => Close();

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
        // (istersen burada state temizliği de yapabilirsin)
    }
}
