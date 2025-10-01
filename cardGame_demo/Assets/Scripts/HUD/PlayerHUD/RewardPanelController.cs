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
    [SerializeField] private Button mapButton;                    // ← YENİ: Accept sonrası görünecek

    [Header("Settings")]
    [SerializeField] private EconomyConfig economy;               // bustPenalty vs.
    [SerializeField] private bool hideOnAccept = false;           // ← Accept’te panel kapanmasın

    [Header("54-Card Deck Value Mapping")]
    [Tooltip("As değeri (1 veya 11 tipik).")]
    [SerializeField, Min(0)] private int aceValue = 1;
    [Tooltip("J/Q/K değeri (genelde 10).")]
    [SerializeField, Min(0)] private int faceValue = 10;
    [Tooltip("Joker (2 adet) kart değeri.")]
    [SerializeField, Min(0)] private int jokerValue = 11;

    [Header("Events")]
    public UnityEvent<int> onRewardAccepted;  // final coin miktarı (Accept anında)
    public UnityEvent onMapRequested;         // ← YENİ: Map butonuna basılınca fırlar

    [Header("External")]
    [SerializeField] private PlayerWallet wallet; // ← YENİ: ödülü burada cüzdana basacağız

    // --- Runtime ---
    RewardContext ctx;
    readonly List<IRewardRelicEffect> rewardRelics = new();
    System.Random rng = new System.Random();

    List<int> rewardDeckValues;
    int deckIndex;

    bool rewardAccepted;   // Accept’e basıldı mı?
    int acceptedAmount;    // kabul edilen ödül

    void Reset()
    {
        if (!panelRoot) panelRoot = gameObject;

        if (!baseRewardText)  baseRewardText  = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!finalRewardText) finalRewardText = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!progressText)    progressText    = panelRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        if (!drawButton)      drawButton      = panelRoot.GetComponentInChildren<Button>(true);
        if (!acceptButton)    acceptButton    = panelRoot.GetComponentInChildren<Button>(true);
        if (!cancelButton)    cancelButton    = panelRoot.GetComponentInChildren<Button>(true);

        if (!mapButton)       mapButton       = panelRoot.GetComponentInChildren<Button>(true); // varsa yakalar
    }

    void Awake()
    {
        if (!panelRoot) panelRoot = gameObject;

        // Butonlar eksikse otomatik bulmayı dene
        if (!mapButton)
        {
            mapButton = panelRoot.GetComponentInChildren<Button>(true);
            if (mapButton)
                Debug.Log($"[RewardUI] mapButton auto-wired: {mapButton.name}");
            else
                Debug.LogWarning("[RewardUI] mapButton is NULL. Assign it in Inspector!");
        }

        if (drawButton)   drawButton.onClick.AddListener(OnDraw);
        if (acceptButton) acceptButton.onClick.AddListener(OnAccept);
        if (cancelButton) cancelButton.onClick.AddListener(OnCancel);
        if (mapButton)    mapButton.onClick.AddListener(OnMap);

        if (panelRoot.activeSelf) panelRoot.SetActive(false);
        if (mapButton) mapButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Reward ekranını aç ve mini-oyunu başlat.
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

        rewardAccepted = false;
        acceptedAmount = 0;

        BuildAndShuffle54Deck();

        // Başlangıç UI durumu
        if (drawButton)   { drawButton.gameObject.SetActive(true);   drawButton.interactable = true; }
        if (acceptButton) { acceptButton.gameObject.SetActive(true); acceptButton.interactable = true; }
        if (mapButton)    mapButton.gameObject.SetActive(false);

        UpdateUI();
        panelRoot.SetActive(true);
    }

    // === Deck (54) ===
    void BuildAndShuffle54Deck()
    {
        rewardDeckValues = new List<int>(54);

        for (int i = 0; i < 4; i++) rewardDeckValues.Add(aceValue);
        for (int v = 2; v <= 10; v++)
            for (int i = 0; i < 4; i++)
                rewardDeckValues.Add(v);
        for (int i = 0; i < 12; i++) rewardDeckValues.Add(faceValue);
        rewardDeckValues.Add(jokerValue);
        rewardDeckValues.Add(jokerValue);

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
        if (ctx == null) return;

        if (baseRewardText) baseRewardText.text = $"Base Reward: {ctx.baseReward}";

        int sum = RewardService.Sum(ctx.drawnValues);
        int max = Mathf.Max(1, ctx.maxRange);
        bool isCapped = sum >= max;
        if (drawButton) drawButton.interactable = !isCapped && !ctx.isBust;

        if (progressText) progressText.text = $"{sum}/{max}";

        int preview = RewardService.ComputeFinal(ctx, rewardRelics);
        if (finalRewardText) finalRewardText.text = $"Final Reward: {preview}";

        bool isBust = ctx.isBust;

        // Accept öncesi durum
        if (!rewardAccepted)
        {
            if (drawButton)   drawButton.interactable   = !isBust;
            if (acceptButton) acceptButton.interactable = true; // bust olsa da kabul edebilir (ceza uygulanır)
        }
        else
        {
            // Accept sonrası: metinleri kabul edilen miktara sabitle
            if (finalRewardText) finalRewardText.text = $"Final Reward: {acceptedAmount}";
        }
    }

    // === Actions ===
    void OnDraw()
    {
        if (rewardAccepted) return; // accept sonrası çekim yok

        int v = DrawFromDeck();

        if (v == jokerValue)
        {
            // JOKER: şu anki toplamı doğrudan max range'e kapat
            int sum = RewardService.Sum(ctx.drawnValues);
            int capDelta = Mathf.Max(0, ctx.maxRange - sum);

            if (capDelta > 0)
            {
                ctx.drawnValues.Add(capDelta);
                Debug.Log($"[Reward] Joker! Sum {sum} -> {ctx.maxRange} (added +{capDelta})");
            }
            else
            {
                // zaten cap'teyiz veya üstünde (teknik olarak üstü olmamalı)
                Debug.Log("[Reward] Joker çekildi ama toplam zaten max. Değişiklik yok.");
            }
        }
        else
        {
            // normal kart
            ctx.drawnValues.Add(v);
        }

        UpdateUI();
    }

    void OnAccept()
    {
        if (rewardAccepted) return; // çifte tıklamaya karşı

        int final = RewardService.ComputeFinal(ctx, rewardRelics);

        // 1) Cüzdana ekle
        if (wallet != null) wallet.AddCoins(final);

        // 2) Event yayınla (başka sistemler dinlemek isteyebilir)
        onRewardAccepted?.Invoke(final);

        // 3) UI durumunu değiştir
        acceptedAmount = final;
        rewardAccepted = true;

        if (drawButton)   drawButton.gameObject.SetActive(false);
        if (acceptButton) acceptButton.gameObject.SetActive(false);
        if (mapButton)    mapButton.gameObject.SetActive(true);

        // Panel açık kalıyor (hideOnAccept=false)
        UpdateUI();
    }

    void OnCancel()
    {
        // İstersen iptalde paneli kapatıp hiçbir şey yapma
        Close();
    }

    bool mapRequested;   // sınıf alanı

    void OnMap()
    {
        if (mapRequested) { Debug.Log("[RewardUI] Map button clicked again (ignored)."); return; }
        mapRequested = true;

        Debug.Log("[RewardUI] Map button clicked.");

        // 1) Inspector event'ini tetikle (varsa)
        try
        {
            int listeners = onMapRequested != null ? onMapRequested.GetPersistentEventCount() : 0;
            Debug.Log($"[RewardUI] onMapRequested invoke (persistent listeners: {listeners}).");
            onMapRequested?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RewardUI] onMapRequested threw: {ex}");
        }

        // 2) Fallback: GameSessionDirector'ı bul ve çağır
        var director = GameSessionDirector.Instance
                    ?? FindFirstObjectByType<GameSessionDirector>(FindObjectsInactive.Include);

        if (director != null)
        {
            Debug.Log("[RewardUI] Fallback → GameSessionDirector.OpenMapFromReward()");
            director.OpenMapFromReward();
        }
        else
        {
            Debug.LogError("[RewardUI] GameSessionDirector not found! Cannot open map.");
        }

        // 3) Paneli kapat
        Close();
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }
}
