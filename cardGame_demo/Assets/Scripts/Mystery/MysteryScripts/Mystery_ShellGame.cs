using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class Mystery_ShellGame : MonoBehaviour, IMystery
{
    [Header("Auto-Wire")]
    [SerializeField] private bool autoWireOnAwake = true;

    [Tooltip("Bulunamazsa isimle aramak için opsiyonel kök objeler")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private Transform cupsRoot;

    [Header("Stake Buttons")]
    [SerializeField] private Button stake25Btn;
    [SerializeField] private Button stake50Btn;
    [SerializeField] private Button stake100Btn;

    [Header("Targets (3 cups)")]
    [SerializeField] private Button[] targetButtons = new Button[3];          // 3 adet
    [SerializeField] private Transform[] targetTransforms = new Transform[3]; // aynı sırada
    public event System.Action<MysteryResult> OnMysteryCompleted; // <-- EKLENDİ
    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text stakeText;

    [Header("Shuffle")]
    [SerializeField, Tooltip("Karıştırma süresi (toplam)")] private float shuffleTotalDuration = 2.0f;
    [SerializeField, Tooltip("Swap sayısı")] private int shuffleSwaps = 10;
    [SerializeField, Tooltip("Tek bir swap animasyon süresi")] private float singleSwapDuration = 0.15f;

    [Header("Rules")]
    [SerializeField, Tooltip("Minimum bahis (coin)")] private int minStake = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // Runtime
    private MysteryContext ctx;
    private GameSessionDirector gsd;
    private PlayerWallet playerWallet;

    private int stake;               // seçilen bahis (coins)
    private int winningIndex = -1;   // 0..2 arası
    private Vector3[] initialPositions;
    private bool inputLocked = false;

    // -------------------------------------------------------------
    // Lifecycle
    private void Awake()
    {
        gsd = GameSessionDirector.Instance;
        playerWallet = GetWallet();

        if (autoWireOnAwake)
        {
            TryAutoWireSceneObjects();
            SafeWireButtonListeners();
            CacheInitialPositions();

            SetTargetsInteractable(false);
            UpdateWalletAndStakeText(GetWalletCoins());
            infoText?.SetText("Choose your bet: 25% / 50% / 100%");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && autoWireOnAwake)
            TryAutoWireSceneObjects();
    }
#endif

    private void OnDestroy()
    {
        SafeUnwireButtonListeners();
    }

    // Mystery entrypoint
    public void Init(MysteryContext ctx)
    {
        this.ctx = ctx;
        this.gsd = GameSessionDirector.Instance;
        this.playerWallet = GetWallet();

        SetTargetsInteractable(false);
        UpdateWalletAndStakeText(GetWalletCoins());
        infoText?.SetText("Choose your bet: 25% / 50% / 100%");
    }

    // -------------------------------------------------------------
    // Wallet helpers
    private PlayerWallet GetWallet()
    {
        // Önce Singleton
        if (PlayerWallet.Instance != null)
        {
            playerWallet = PlayerWallet.Instance;
            return playerWallet;
        }

        // Singleton yoksa sahnede ara
        var all = FindObjectsOfType<PlayerWallet>(true);
        if (all.Length == 0)
            return null;

        if (all.Length > 1)
        {
            // DDOL sahnesindeki varsa onu tercih et
            var ddol = all.FirstOrDefault(w => w.gameObject.scene.name == "DontDestroyOnLoad");
            playerWallet = ddol ?? all[0];
            if (debugLogs)
                Debug.LogWarning($"[ShellGame] Birden fazla PlayerWallet bulundu ({all.Length}). " +
                                 $"Kullanılan='{playerWallet.gameObject.name}' (scene={playerWallet.gameObject.scene.name}). Tekilleştirmen önerilir.");
            return playerWallet;
        }

        playerWallet = all[0];
        if (debugLogs)
            Debug.LogWarning($"[ShellGame] PlayerWallet.Instance yoktu; sahneden '{playerWallet.gameObject.name}' kullanılıyor.");
        return playerWallet;
    }

    private int GetWalletCoins()
    {
        var w = GetWallet();
        return w != null ? w.GetCoins() : 0;
    }

    private bool TrySpend(int amount, string reason)
    {
        var w = GetWallet();
        if (w == null) { infoText?.SetText("Wallet not ready."); return false; }
        int before = w.GetCoins();
        if (before < amount)
        {
            if (debugLogs) Debug.LogWarning($"[ShellGame] TrySpend({amount}) YETERSİZ. before={before}. reason={reason}");
            return false;
        }
        w.AddCoins(-amount);
        int after = w.GetCoins();
        if (debugLogs) Debug.Log($"[ShellGame] Spend {amount} reason={reason} {before}->{after}");
        UpdateWalletAndStakeText(after);
        return true;
    }

    private void Payout(int amount, string reason)
    {
        var w = GetWallet();
        if (w == null) { infoText?.SetText("Wallet not ready."); return; }
        int before = w.GetCoins();
        w.AddCoins(amount);
        int after = w.GetCoins();
        if (debugLogs) Debug.Log($"[ShellGame] Payout {amount} reason={reason} {before}->{after}");
        UpdateWalletAndStakeText(after);
    }

    // -------------------------------------------------------------
    // UI wiring
    private void TryAutoWireSceneObjects()
    {
        if (!uiRoot)   uiRoot   = transform;
        if (!cupsRoot) cupsRoot = transform;

        // Stake buttons
        if (!stake25Btn)  stake25Btn  = FindIn(uiRoot, "Stake25", true)?.GetComponent<Button>()  ?? FindOfTypeInScene<Button>("Stake25");
        if (!stake50Btn)  stake50Btn  = FindIn(uiRoot, "Stake50", true)?.GetComponent<Button>()  ?? FindOfTypeInScene<Button>("Stake50");
        if (!stake100Btn) stake100Btn = FindIn(uiRoot, "Stake100", true)?.GetComponent<Button>() ?? FindOfTypeInScene<Button>("Stake100");

        // UI Texts
        if (!infoText)  infoText  = (FindIn(uiRoot, "InfoText", true)?.GetComponent<TMP_Text>())  ?? FindOfTypeInScene<TMP_Text>("InfoText");
        if (!stakeText) stakeText = (FindIn(uiRoot, "StakeText", true)?.GetComponent<TMP_Text>()) ?? FindOfTypeInScene<TMP_Text>("StakeText");

        // Cups / Targets
        if (targetButtons == null || targetButtons.Length != 3 || targetButtons.Any(b => b == null))
        {
            var btns = (cupsRoot ? cupsRoot.GetComponentsInChildren<Button>(true) : FindObjectsOfType<Button>(true))
                        .Where(b => b.name.ToLower().Contains("cup") || b.name.ToLower().Contains("target"))
                        .Take(3).ToArray();
            if (btns.Length == 3) targetButtons = btns;
        }

        if (targetTransforms == null || targetTransforms.Length != 3 || targetTransforms.Any(t => t == null))
        {
            if (targetButtons != null && targetButtons.Length == 3 && targetButtons.All(b => b != null))
                targetTransforms = targetButtons.Select(b => b.transform).ToArray();
        }
    }

    private static Transform FindIn(Transform root, string name, bool includeInactive)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive))
            if (t.name == name) return t;
        return null;
    }

    private static T FindOfTypeInScene<T>(string nameContains) where T : Component
    {
        var all = Object.FindObjectsOfType<T>(true);
        if (string.IsNullOrEmpty(nameContains)) return all.FirstOrDefault();
        var lc = nameContains.ToLower();
        return all.FirstOrDefault(c => c.name.ToLower().Contains(lc));
    }

    private void SafeWireButtonListeners()
    {
        SafeUnwireButtonListeners();

        if (stake25Btn)  stake25Btn.onClick.AddListener(() => SelectStake(0.25f));
        if (stake50Btn)  stake50Btn.onClick.AddListener(() => SelectStake(0.50f));
        if (stake100Btn) stake100Btn.onClick.AddListener(() => SelectStake(1.00f));

        if (targetButtons != null)
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                var btn = targetButtons[i];
                int ix = i;
                if (btn) btn.onClick.AddListener(() => OnPick(ix));
            }
        }
    }

    private void SafeUnwireButtonListeners()
    {
        if (stake25Btn)  stake25Btn.onClick.RemoveAllListeners();
        if (stake50Btn)  stake50Btn.onClick.RemoveAllListeners();
        if (stake100Btn) stake100Btn.onClick.RemoveAllListeners();

        if (targetButtons != null)
            foreach (var b in targetButtons)
                if (b) b.onClick.RemoveAllListeners();
    }

    private void CacheInitialPositions()
    {
        if (targetTransforms == null) return;
        if (initialPositions == null || initialPositions.Length != targetTransforms.Length)
            initialPositions = new Vector3[targetTransforms.Length];

        for (int i = 0; i < targetTransforms.Length; i++)
            if (targetTransforms[i])
                initialPositions[i] = targetTransforms[i].position;
    }

    // -------------------------------------------------------------
    // Game flow
    private void SelectStake(float fraction)
    {
        if (inputLocked) return;

        if (GetWallet() == null)
        {
            if (debugLogs) Debug.LogWarning("[ShellGame] PlayerWallet bulunamadı.");
            infoText?.SetText("Wallet not ready.");
            return;
        }

        int walletBefore = GetWalletCoins();
        int desired = Mathf.FloorToInt(walletBefore * fraction);

        if (desired < minStake)
        {
            infoText?.SetText($"Minimum stake is {minStake}. Your balance: {walletBefore}");
            return;
        }

        // Harcama başarısızsa devam etme
        if (!TrySpend(desired, "bet placed"))
        {
            infoText?.SetText($"Not enough coins to bet {desired}.");
            return;
        }

        stake = desired;

        // Kilitle & shuffle
        inputLocked = true;
        SetStakeButtonsInteractable(false);
        SetTargetsInteractable(false);

        infoText?.SetText($"Bet placed: -{stake}. Shuffling...");
        StartCoroutine(ShuffleRoutine());
    }

    private IEnumerator ShuffleRoutine()
    {
        inputLocked = true;

        SetTargetsInteractable(false);
        SetStakeButtonsInteractable(false);
        infoText?.SetText("Shuffling...");

        // Kazanan bardağı seç (0..2)
        winningIndex = (ctx != null && ctx.rng != null) ? ctx.rng.Next(0, 3) : Random.Range(0, 3);

        // Güvenlik
        if (targetTransforms == null || targetTransforms.Length < 3)
        {
            Debug.LogWarning("[Mystery_ShellGame] targetTransforms eksik.");
            yield break;
        }

        int swaps = Mathf.Max(1, shuffleSwaps);

        for (int s = 0; s < swaps; s++)
        {
            int a = (ctx != null && ctx.rng != null) ? ctx.rng.Next(0, 3) : Random.Range(0, 3);
            int b;
            do { b = (ctx != null && ctx.rng != null) ? ctx.rng.Next(0, 3) : Random.Range(0, 3); } while (b == a);

            if (winningIndex == a) winningIndex = b;
            else if (winningIndex == b) winningIndex = a;

            Vector3 startA = targetTransforms[a].position;
            Vector3 startB = targetTransforms[b].position;
            float t = 0f;
            while (t < singleSwapDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / singleSwapDuration);
                targetTransforms[a].position = Vector3.Lerp(startA, startB, u);
                targetTransforms[b].position = Vector3.Lerp(startB, startA, u);
                yield return null;
            }

            targetTransforms[a].position = startB;
            targetTransforms[b].position = startA;
        }

        infoText?.SetText("Pick a cup!");
        SetTargetsInteractable(true);
        inputLocked = false;
    }
    private void OnPick(int pickIndex)
    {
        if (inputLocked) return;
        if (stake < minStake) return;

        inputLocked = true;
        SetTargetsInteractable(false);

        bool win = (pickIndex == winningIndex);

        if (win)
        {
            // Zaten stake kesilmişti; şimdi 2x ödeyip net +stake yapıyoruz
            Payout(stake * 2, "win payout (2x)");
            infoText?.SetText($"You WIN! payout +{stake * 2} (profit +{stake})");

            // ---> EVENT: coinleri zaten cüzdana yazdık, o yüzden 0 gönderiyoruz
            OnMysteryCompleted?.Invoke(new MysteryResult {
                // outcome = MysteryOutcome.Nothing, // projenizdeki "None/Nothing" ne ise onu kullanın
                coins = 0
            });

            ctx?.CompleteNothing(); // istersen kaldır
        }
        else
        {
            infoText?.SetText($"You LOSE! (lost {stake})");

            // ---> EVENT: kayıp da tamamlandı sinyali
            OnMysteryCompleted?.Invoke(new MysteryResult {
                // outcome = MysteryOutcome.Nothing,
                coins = 0
            });

            ctx?.CompleteNothing(); // istersen kaldır
        }

        UpdateWalletAndStakeText(GetWalletCoins());
        stake = 0;
        inputLocked = false;
        SetStakeButtonsInteractable(true);
        infoText?.SetText("Choose your bet: 25% / 50% / 100%");
    }

    // -------------------------------------------------------------
    // UI yardımcıları
    private void SetStakeButtonsInteractable(bool v)
    {
        if (stake25Btn)  stake25Btn.interactable  = v;
        if (stake50Btn)  stake50Btn.interactable  = v;
        if (stake100Btn) stake100Btn.interactable = v;
    }

    private void SetTargetsInteractable(bool v)
    {
        if (targetButtons == null) return;
        foreach (var b in targetButtons)
            if (b) b.interactable = v;
    }

    private void UpdateWalletAndStakeText(int wallet)
    {
        if (stakeText) stakeText.SetText($"Stake: {stake}");
        if (infoText)  infoText.SetText($"Balance: {wallet} — pick your bet (25/50/100)");
    }
}
