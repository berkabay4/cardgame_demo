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
    [SerializeField] private Button[] targetButtons = new Button[3];    // 3 adet
    [SerializeField] private Transform[] targetTransforms = new Transform[3]; // aynı sırada

    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text stakeText;

    [Header("Shuffle")]
    [SerializeField, Tooltip("Karıştırma süresi (toplam)")] private float shuffleTotalDuration = 2.0f;
    [SerializeField, Tooltip("Swap sayısı")] private int shuffleSwaps = 10;
    [SerializeField, Tooltip("Tek bir swap animasyon süresi")] private float singleSwapDuration = 0.15f;

    [Header("Rules")]
    [SerializeField, Tooltip("Minimum bahis (coin)")] private int minStake = 1;

    private MysteryContext ctx;
    private GameSessionDirector gsd;
    private PlayerWallet playerWallet;

    private int stake;               // seçilen bahis coins
    private int winningIndex = -1;   // 0..2 arası
    private Vector3[] initialPositions;
    private bool inputLocked = false;

    // --- SCENE AUTO-WIRING ---------------------------------------------------
    private void Awake()
    {
        // Bu iki satır EKLENDİ: sahne açılır açılmaz referansları al
        gsd = GameSessionDirector.Instance;
        playerWallet = PlayerWallet.Instance;

        if (autoWireOnAwake)
        {
            TryAutoWireSceneObjects();
            SafeWireButtonListeners();
            CacheInitialPositions();

            // Cüzdanı göster (Init beklemeden)
            int wallet = playerWallet ? playerWallet.GetCoins() : 0;
            SetTargetsInteractable(false);
            UpdateWalletAndStakeText(wallet);
            infoText?.SetText("Choose your bet: 25% / 50% / 100%");
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editörde sahnede düzenleme yaparken önizleme için hafif auto-wire (play değilken)
        if (!Application.isPlaying && autoWireOnAwake)
        {
            TryAutoWireSceneObjects();
        }
    }
#endif

    private void OnDestroy()
    {
        // Sahne değişimlerinde/yeniden yüklemede çift dinleyiciyi engelle
        SafeUnwireButtonListeners();
    }

    private void TryAutoWireSceneObjects()
    {
        // Kökler boşsa tüm sahnede arayacağız
        if (!uiRoot)   uiRoot   = transform;   // en azından kendi altında ara
        if (!cupsRoot) cupsRoot = transform;

        // --- Stake Buttons ---
        if (!stake25Btn)  stake25Btn  = FindIn(uiRoot, "Stake25", true)?.GetComponent<Button>() ?? FindOfTypeInScene<Button>("Stake25");
        if (!stake50Btn)  stake50Btn  = FindIn(uiRoot, "Stake50", true)?.GetComponent<Button>() ?? FindOfTypeInScene<Button>("Stake50");
        if (!stake100Btn) stake100Btn = FindIn(uiRoot, "Stake100", true)?.GetComponent<Button>() ?? FindOfTypeInScene<Button>("Stake100");

        // --- UI Texts ---
        if (!infoText)  infoText  = (FindIn(uiRoot, "InfoText", true)?.GetComponent<TMP_Text>())  ?? FindOfTypeInScene<TMP_Text>("InfoText");
        if (!stakeText) stakeText = (FindIn(uiRoot, "StakeText", true)?.GetComponent<TMP_Text>()) ?? FindOfTypeInScene<TMP_Text>("StakeText");

        // --- Cups / Targets ---
        // Eğer hedef dizileri boş ya da eksikse, cupsRoot altındaki ilk 3 Button/Transform’u topla
        if (targetButtons == null || targetButtons.Length != 3 || targetButtons.Any(b => b == null))
        {
            var btns = (cupsRoot ? cupsRoot.GetComponentsInChildren<Button>(true) : FindObjectsOfType<Button>(true))
                        .Where(b => b.name.ToLower().Contains("cup") || b.name.ToLower().Contains("target"))
                        .Take(3)
                        .ToArray();
            if (btns.Length == 3) targetButtons = btns;
        }

        if (targetTransforms == null || targetTransforms.Length != 3 || targetTransforms.Any(t => t == null))
        {
            // Transform’ları butonların transform’larından türet
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
        // Önce sil
        SafeUnwireButtonListeners();

        // Stake buttons
        if (stake25Btn)  stake25Btn.onClick.AddListener(() => SelectStake(0.25f));
        if (stake50Btn)  stake50Btn.onClick.AddListener(() => SelectStake(0.50f));
        if (stake100Btn) stake100Btn.onClick.AddListener(() => SelectStake(1.00f));

        // Cup pick
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
    // ------------------------------------------------------------------------

    public void Init(MysteryContext ctx)
    {
        this.ctx = ctx;
        // YİNE de burada güncelle (runtime’da sahne değişmiş olabilir)
        this.gsd = GameSessionDirector.Instance;
        this.playerWallet = PlayerWallet.Instance;

        SetTargetsInteractable(false);
        UpdateWalletAndStakeText(playerWallet ? playerWallet.GetCoins() : 0);
        infoText?.SetText("Choose your bet: 25% / 50% / 100%");
    }

    private void SelectStake(float fraction)
    {
        if (inputLocked) return;

        // Emniyet: Init çağrılmasa bile çalışsın
        if (playerWallet == null) playerWallet = PlayerWallet.Instance;

        if (playerWallet == null)
        {
            Debug.LogWarning("[Mystery_ShellGame] PlayerWallet bulunamadı.");
            infoText?.SetText("Wallet not ready.");
            return;
        }

        int wallet = playerWallet.GetCoins();
        int desired = Mathf.FloorToInt(wallet * fraction);
        if (desired < minStake)
        {
            infoText?.SetText($"Minimum stake is {minStake}. Your balance: {wallet}");
            return;
        }

        stake = desired;
        UpdateWalletAndStakeText(wallet);

        StartCoroutine(ShuffleRoutine());
    }

    private IEnumerator ShuffleRoutine()
    {
        inputLocked = true;

        // Hedefleri kilitle & bahis butonlarını kilitle
        SetTargetsInteractable(false);
        SetStakeButtonsInteractable(false);

        infoText?.SetText("Shuffling...");

        // Kazanan bardağı seç (0..2)
        winningIndex = ctx != null && ctx.rng != null ? ctx.rng.Next(0, 3) : Random.Range(0, 3);

        // Karıştırma
        int swaps = Mathf.Max(1, shuffleSwaps);

        // Çalışma kopyaları
        if (targetTransforms == null || targetTransforms.Length < 3)
        {
            Debug.LogWarning("[Mystery_ShellGame] targetTransforms eksik.");
            yield break;
        }

        Vector3[] pos = new Vector3[targetTransforms.Length];
        for (int i = 0; i < pos.Length; i++) pos[i] = targetTransforms[i].position;

        for (int s = 0; s < swaps; s++)
        {
            int a = (ctx != null && ctx.rng != null) ? ctx.rng.Next(0, 3) : Random.Range(0, 3);
            int b;
            do { b = (ctx != null && ctx.rng != null) ? ctx.rng.Next(0, 3) : Random.Range(0, 3); } while (b == a);

            // winningIndex swap'tan etkilenir:
            if (winningIndex == a) winningIndex = b;
            else if (winningIndex == b) winningIndex = a;

            // Basit lerp animasyonu
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

            // Son konumları netle
            targetTransforms[a].position = startB;
            targetTransforms[b].position = startA;
        }

        // Shuffle bitti -> hedefler aç
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
            // Kazanç: net +stake (1:1 oran)
            infoText?.SetText($"You WIN! +{stake} coins");
            ctx?.CompleteCoins(stake);
        }
        else
        {
            // Kayıp: anında düş
            infoText?.SetText($"You LOSE! -{stake} coins");
            if (playerWallet != null) playerWallet.AddCoins(-stake);
            ctx?.CompleteNothing();
        }

        // Sonraki tur için (istersen) stake’i sıfırlayabilirsin:
        // stake = 0; UpdateWalletAndStakeText(playerWallet ? playerWallet.GetCoins() : 0);
    }

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
        if (stakeText)
            stakeText.SetText($"Stake: {stake}");

        // gsd kontrolüne gerek yok; direkt göster
        if (infoText)
            infoText.SetText($"Balance: {wallet} — pick your bet (25/50/100)");
    }
}
