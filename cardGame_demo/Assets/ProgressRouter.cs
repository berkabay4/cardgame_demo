// ProgressRouter.cs
using TMPro;
using UnityEngine;

public class ProgressRouter : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private TMP_Text playerDefText;
    [SerializeField] private TMP_Text playerAtkText;

    [Header("Enemy")]
    [SerializeField] private TMP_Text enemyDefText;
    [SerializeField] private TMP_Text enemyAtkText;

    [SerializeField] private string format = "{0} / {1}";
    [SerializeField] private bool logWhenUpdated = false;

    public void OnProgress(Actor actor, PhaseKind phase, int current, int max)
    {
        TMP_Text t = null;
        if (actor == Actor.Player && phase == PhaseKind.Defense) t = playerDefText;
        else if (actor == Actor.Player && phase == PhaseKind.Attack) t = playerAtkText;
        else if (actor == Actor.Enemy  && phase == PhaseKind.Defense) t = enemyDefText;
        else if (actor == Actor.Enemy  && phase == PhaseKind.Attack) t = enemyAtkText;

        if (!t) { Debug.LogWarning($"[ProgressRouter] Missing TMP for {actor}-{phase}."); return; }

        t.SetText(format, current, max);
        if (logWhenUpdated) Debug.Log($"[ProgressRouter] {actor}-{phase} => {current}/{max}");
    }

    public void InitAll(int max)
    {
        if (playerDefText) playerDefText.SetText(format, 0, max);
        if (playerAtkText) playerAtkText.SetText(format, 0, max);
        if (enemyDefText)  enemyDefText.SetText(format, 0, max);
        if (enemyAtkText)  enemyAtkText.SetText(format, 0, max);
    }
}
