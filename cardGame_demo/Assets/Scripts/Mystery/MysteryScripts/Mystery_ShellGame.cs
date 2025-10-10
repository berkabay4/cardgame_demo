using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Mystery_ShellGame : MonoBehaviour, IMystery
{
    [Header("Stake Buttons")]
    [SerializeField] private Button stake25Btn;
    [SerializeField] private Button stake50Btn;
    [SerializeField] private Button stake100Btn;

    [Header("Targets (3 cups)")]
    [SerializeField] private Button[] targetButtons = new Button[3]; // 3 adet
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

    public void Init(MysteryContext ctx)
    {
        this.ctx = ctx;
        this.gsd = GameSessionDirector.Instance;
        this.playerWallet = PlayerWallet.Instance;

        // UI başlangıç
        SetTargetsInteractable(false);
        UpdateWalletAndStakeText(0);

        // Buton wiring
        stake25Btn.onClick.AddListener(() => SelectStake(0.25f));
        stake50Btn.onClick.AddListener(() => SelectStake(0.50f));
        stake100Btn.onClick.AddListener(() => SelectStake(1.00f));

        for (int i = 0; i < targetButtons.Length; i++)
        {
            int ix = i;
            targetButtons[i].onClick.AddListener(() => OnPick(ix));
        }

        // Pozisyonları kaydet
        initialPositions = new Vector3[targetTransforms.Length];
        for (int i = 0; i < targetTransforms.Length; i++)
            initialPositions[i] = targetTransforms[i].position;

        infoText?.SetText("Choose your bet: 25% / 50% / 100%");
    }

    private void SelectStake(float fraction)
    {
        if (inputLocked) return;

        int wallet = playerWallet != null ? playerWallet.GetCoins() : 0;
        int desired = Mathf.FloorToInt(wallet * fraction);
        if (desired < minStake)
        {
            infoText?.SetText($"Minimum stake is {minStake}. Your balance: {wallet}");
            return;
        }

        stake = desired;
        UpdateWalletAndStakeText(wallet);

        // Bahis seçildi -> shuffle başlat, sonra hedefleri aç
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
        winningIndex = ctx.rng.Next(0, 3);

        // Karıştırma
        int swaps = Mathf.Max(1, shuffleSwaps);

        // Çalışma kopyaları
        Vector3[] pos = new Vector3[targetTransforms.Length];
        for (int i = 0; i < pos.Length; i++) pos[i] = targetTransforms[i].position;

        for (int s = 0; s < swaps; s++)
        {
            int a = ctx.rng.Next(0, 3);
            int b;
            do { b = ctx.rng.Next(0, 3); } while (b == a);

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
            Vector3 temp = targetTransforms[a].position;
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
            // Kazancı GSD üzerinde outcome=Coins olarak raporlayacağız
            // Map butonu görünür olacak; tıklanınca cüzdana eklenecek.
            ctx.CompleteCoins(stake);
        }
        else
        {
            // Kayıp: anında düş (ReportMysteryFinished negatif işlemediği için)
            infoText?.SetText($"You LOSE! -{stake} coins");
            if (playerWallet != null) playerWallet.AddCoins(-stake);

            // Coins vermiyoruz
            ctx.CompleteNothing();
        }
    }

    private void SetStakeButtonsInteractable(bool v)
    {
        if (stake25Btn) stake25Btn.interactable = v;
        if (stake50Btn) stake50Btn.interactable = v;
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
        // infoText içinde cüzdanı da göstermek istersen:
        if (infoText && gsd != null)
            infoText.SetText($"Balance: {wallet} — pick your bet (25/50/100)");
    }
}
