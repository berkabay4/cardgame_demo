// CombatFlowAdapter.cs
using UnityEngine;

public class CombatFlowAdapter : MonoBehaviour
{
    [SerializeField] RunContext run;
    [SerializeField] CombatDirector combatDirector;
    [SerializeField] UnityEngine.UI.Button startTurnButton;

    void Awake()
    {
        if (!combatDirector) combatDirector = CombatDirector.Instance;
        if (startTurnButton) startTurnButton.onClick.AddListener(OnStartPressed);

        // Savaş bitti eventlerini yakala
        combatDirector.onGameWin.AddListener(OnWin);
        combatDirector.onGameOver.AddListener(OnLose);
    }

    void OnDestroy()
    {
        if (!combatDirector) return;
        combatDirector.onGameWin.RemoveListener(OnWin);
        combatDirector.onGameOver.RemoveListener(OnLose);
        if (startTurnButton) startTurnButton.onClick.RemoveListener(OnStartPressed);
    }

    void OnStartPressed()
    {
        // Sahnede düşmanlar spawn edilmiş olmalı
        combatDirector.StartGame(); // GameDirector içindeki akışı başlatır
    }

    void OnWin()
    {
        // Basit ödül hesaplama: AliveEnemies başlangıç sayısına göre vs.
        // Şimdilik RunContext’ten default/konfigüre geleni kullanıyoruz.
        int coins = run != null ? Mathf.Max(0, run.pendingCoins) : 0;
        GameSessionDirector.Instance.ReportCombatFinished(true, coins);
    }

    void OnLose()
    {
        GameSessionDirector.Instance.ReportCombatFinished(false, 0);
    }
}
