using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardOrchestrator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameDirector director;
    [SerializeField] private EconomyConfig economy;
    [SerializeField] private RewardPanelController rewardPanel;
    [SerializeField] private PlayerWallet wallet;

    [Header("Reward Policy")]
    [SerializeField] private ScriptableObject rewardPolicyAsset; // IBattleRewardPolicy
    IBattleRewardPolicy rewardPolicy;

    // runtime cache
    private PlayerData playerDataCached;
    private bool subscribedPlayerEvents;

    // ---- lifecycle ----
    void Reset()
    {
        director    = GameDirector.Instance ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
        wallet      = PlayerWallet.Instance ?? FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        rewardPanel = FindFirstObjectByType<RewardPanelController>(FindObjectsInactive.Include);
        economy     = FindFirstObjectByType<EconomyConfig>(FindObjectsInactive.Include);
    }

    void Awake()
    {
        rewardPolicy = rewardPolicyAsset as IBattleRewardPolicy;

        if (director == null)
            director = GameDirector.Instance ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
        if (wallet == null)
            wallet = PlayerWallet.Instance ?? FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
    }

    void OnEnable()
    {
        // GameDirector sinyalleri
        if (director != null) director.onGameWin.AddListener(OnGameWin);

        // Panel sinyali
        if (rewardPanel != null) rewardPanel.onRewardAccepted.AddListener(OnRewardAccepted);

        // Player’ı çöz ve event’ine abone ol
        TryBindPlayerNow();
        // Director hazır sinyali (oyuncu geç doğarsa)
        GameDirector.ContextReady += OnContextReady;

        // Son çare: kısa aralıklarla Player.Instance’ı yokla (spawn sırası bilinmiyorsa)
        if (Player.Instance == null) InvokeRepeating(nameof(TryBindPlayerNow), 0.2f, 0.2f);
    }

    void OnDisable()
    {
        if (director != null) director.onGameWin.RemoveListener(OnGameWin);
        if (rewardPanel != null) rewardPanel.onRewardAccepted.RemoveListener(OnRewardAccepted);

        GameDirector.ContextReady -= OnContextReady;

        UnsubscribePlayerEvents();
        CancelInvoke(nameof(TryBindPlayerNow));
    }

    void Start()
    {
        // Cüzdanı PlayerData’dan başlat (sadece ilk sahnede)
        if (wallet != null && GetPlayerData() != null && wallet.GetCoins() == 0)
            wallet.InitFromPlayerData(GetPlayerData());
    }

    // ---- player binding ----
    void OnContextReady() => TryBindPlayerNow();

    void TryBindPlayerNow()
    {
        var p = Player.Instance;
        if (p == null) return;

        // PlayerData cachele
        playerDataCached = p.Data;

        // Player event’ine tek sefer abone ol
        if (!subscribedPlayerEvents)
        {
            p.onPlayerDataChanged.AddListener(OnPlayerDataChanged);
            subscribedPlayerEvents = true;
        }

        // Artık bulduk; retry’ı durdur
        CancelInvoke(nameof(TryBindPlayerNow));
    }

    void UnsubscribePlayerEvents()
    {
        if (!subscribedPlayerEvents) return;
        var p = Player.Instance;
        if (p != null) p.onPlayerDataChanged.RemoveListener(OnPlayerDataChanged);
        subscribedPlayerEvents = false;
    }

    void OnPlayerDataChanged(PlayerData newData)
    {
        playerDataCached = newData;
        // İstersen burada loglayabilirsin
        // Debug.Log($"[RewardOrchestrator] PlayerData updated → {newData?.name}");
    }

    PlayerData GetPlayerData()
    {
        // Öncelik: Player singleton’daki canlı data
        if (Player.Instance?.Data != null) return Player.Instance.Data;
        // Değilse cache
        if (playerDataCached != null) return playerDataCached;
        // Son çare: sahnede bir Player bul ve al
        playerDataCached = FindFirstObjectByType<Player>(FindObjectsInactive.Include)?.Data;
        return playerDataCached;
    }

    // ---- main flow ----
    void OnGameWin()
    {
        if (rewardPanel == null || economy == null)
        {
            Debug.LogWarning("[RewardOrchestrator] RewardPanel/Economy eksik. Ödül paneli açılamadı.");
            return;
        }

        var pData = GetPlayerData();
        if (pData == null)
        {
            Debug.LogWarning("[RewardOrchestrator] PlayerData bulunamadı (Player.Instance yok?). Panel yine açılıyor fakat range default 21 kullanılabilir.");
        }

        int baseReward = rewardPolicy != null ? rewardPolicy.GetBaseReward(director) : 100;
        var relics = CollectRewardRelicEffects();

        rewardPanel.Open(baseReward, pData, economy, relics);
    }

    void OnRewardAccepted(int finalCoins)
    {
        if (wallet == null)
        {
            Debug.LogWarning("[RewardOrchestrator] Wallet yok. Coin eklenemedi.");
            return;
        }
        wallet.AddCoins(finalCoins);
        Debug.Log($"[RewardOrchestrator] Reward accepted → +{finalCoins} coin. Total={wallet.GetCoins()}");
    }

    IEnumerable<IRewardRelicEffect> CollectRewardRelicEffects()
    {
        var list = new List<IRewardRelicEffect>();
        var rm = RelicManager.Instance;
        if (rm == null) return list;

        foreach (var rt in rm.All)
        {
            if (rt == null || rt.isEnabled != true || rt.def?.effects == null) continue;
            foreach (var eff in rt.def.effects)
                if (eff is IRewardRelicEffect re) list.Add(re);
        }
        return list;
    }
}
