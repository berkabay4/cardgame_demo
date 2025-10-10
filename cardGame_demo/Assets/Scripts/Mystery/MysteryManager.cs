// MysteryManager.cs
using UnityEngine;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public class MysteryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mapButtonRoot; // butonun parent'ı veya butonun kendisi
    [SerializeField] private Button mapButton;         // UnityEngine.UI.Button

    // Scene-wide event: IMystery -> (bu sahne) MysteryManager
    public static event Action<MysteryResult> MysteryCompleted;

    private MysteryResult? _pendingResult;

    private void Awake()
    {
        if (mapButtonRoot) mapButtonRoot.SetActive(false);
        if (mapButton)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(OnClickReturn);
        }
    }

    private void OnEnable()
    {
        MysteryCompleted += HandleMysteryCompleted;
    }

    private void OnDisable()
    {
        MysteryCompleted -= HandleMysteryCompleted;
    }

    private void HandleMysteryCompleted(MysteryResult result)
    {
        _pendingResult = result;

        // Tamamlanınca butonu göster
        if (mapButtonRoot && !mapButtonRoot.activeSelf)
            mapButtonRoot.SetActive(true);
    }

    private void OnClickReturn()
    {
        if (!_pendingResult.HasValue)
        {
            Debug.LogWarning("[MysteryManager] Return clicked but no result yet.");
            return;
        }

        var res = _pendingResult.Value;

        // Çift tıklamaya karşı güvenlik
        if (mapButton) mapButton.interactable = false;

        // Raporla -> GSD ilgili sahneyi yükler (Map/Combat/Treasure)
        var gsd = GameSessionDirector.Instance;
        if (gsd != null)
        {
            gsd.ReportMysteryFinished(res.outcome, res.coins);
        }
        else
        {
            Debug.LogError("[MysteryManager] GameSessionDirector.Instance null! Falling back to map.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
        }
    }

    // IMystery handler'larından çağrılacak yardımcı (static)
    public static void RaiseCompleted(MysteryResult result)
    {
        MysteryCompleted?.Invoke(result);
    }
}
