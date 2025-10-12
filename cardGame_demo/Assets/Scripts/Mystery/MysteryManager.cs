using UnityEngine;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public class MysteryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mapButtonRoot; // buton parent'ı (veya butonun kendisi)
    [SerializeField] private Button mapButton;         // UnityEngine.UI.Button

    [Header("Behavior")]
    [Tooltip("TRUE ise buton sahne başında görünür ve butona basıldığında direkt MapScene açılır.")]
    [SerializeField] private bool showReturnAtStart = false;

    [Tooltip("Manuel modda (showReturnAtStart=TRUE) yüklenecek sahne adı.")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("Event Source (Optional)")]
    [Tooltip("Otomatik modda event dinlemek için IMysteryEvents uygulayan bir script referansı (boş bırakılırsa sahnede otomatik aranır).")]
    [SerializeField] private MonoBehaviour mysteryEventSource; // should implement IMysteryEvents

    // Scene-wide legacy event: IMystery -> (bu sahne) MysteryManager
    public static event Action<MysteryResult> MysteryCompleted;

    private MysteryResult? _pendingResult;

    // IMysteryEvents aboneliği için tutucu
    private IMystery _mysteryInterface;

    private void Awake()
    {
        if (mapButton)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(OnClickReturn);
        }

        if (showReturnAtStart)
        {
            // Manuel mod: başlangıçta görünür, herhangi bir sonucu bekleme.
            if (mapButtonRoot) mapButtonRoot.SetActive(true);
        }
        else
        {
            // Otomatik mod: başlangıçta gizle.
            if (mapButtonRoot) mapButtonRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!showReturnAtStart)
        {
            // Otomatik modda IMysteryEvents arayıp dinle.
            TryBindMysteryEvents();
        }

        // Geriye dönük uyumluluk: static event’i de dinle
        MysteryCompleted += HandleMysteryCompleted;
    }

    private void OnDisable()
    {
        // Static event'ten ayrıl
        MysteryCompleted -= HandleMysteryCompleted;

        // IMysteryEvents'ten ayrıl
        if (_mysteryInterface != null)
        {
            _mysteryInterface.OnMysteryCompleted -= HandleMysteryCompleted;
            _mysteryInterface = null;
        }
    }

    private void TryBindMysteryEvents()
    {
        // Önce inspector’dan verilen referansı dene
        if (mysteryEventSource is IMystery evtFromField)
        {
            _mysteryInterface = evtFromField;
            _mysteryInterface.OnMysteryCompleted += HandleMysteryCompleted;
            return;
        }

        // Bulunamazsa sahnede otomatik ara (ilk bulduğu nesneye bağlanır)
#if UNITY_2023_1_OR_NEWER
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<MonoBehaviour>(false);
#endif
        foreach (var mb in all)
        {
            if (mb is IMystery ie)
            {
                _mysteryInterface = ie;
                _mysteryInterface.OnMysteryCompleted += HandleMysteryCompleted;
                break;
            }
        }
    }

    private void HandleMysteryCompleted(MysteryResult result)
    {
        _pendingResult = result;

        // Tamamlanınca butonu göster (otomatik mod)
        if (!showReturnAtStart && mapButtonRoot && !mapButtonRoot.activeSelf)
            mapButtonRoot.SetActive(true);
    }

    private void OnClickReturn()
    {
        // Çift tıklamaya karşı
        if (mapButton) mapButton.interactable = false;

        if (showReturnAtStart)
        {
            // Manuel mod: direkt MapScene’e dön
            UnityEngine.SceneManagement.SceneManager.LoadScene(mapSceneName);
            return;
        }

        // Otomatik mod: sonuç beklenmediyse uyar ve iptal et
        if (!_pendingResult.HasValue)
        {
            Debug.LogWarning("[MysteryManager] Return clicked but no result yet (automatic mode).");
            if (mapButton) mapButton.interactable = true;
            return;
        }

        var res = _pendingResult.Value;

        // Raporla -> GSD ilgili sahneyi yükler (Map/Combat/Treasure)
        var gsd = GameSessionDirector.Instance;
        if (gsd != null)
        {
            gsd.ReportMysteryFinished(res.outcome, res.coins);
        }
        else
        {
            Debug.LogError("[MysteryManager] GameSessionDirector.Instance null! Falling back to map.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mapSceneName);
        }
    }

    // IMystery handler'larından çağrılacak yardımcı (static) — legacy destek
    public static void RaiseCompleted(MysteryResult result)
    {
        MysteryCompleted?.Invoke(result);
    }
}
