using UnityEngine;
using TMPro;

public class CombatStateHud : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CombatDirector combatDirector;   // <<< DÜZELT: Multi!

    [Header("Single Text Output")]
    [SerializeField] TextMeshProUGUI outputText;
    [SerializeField] bool useRichTextColors = true;

    // Dahili durum
    TurnStep _step = TurnStep.PlayerDef;
    bool _waitingTarget = false;
    string _round = "-";
    int _pDef = -1;
    int _pAtk = -1;
    string _target = "-";
    string _enemyTurn = "-";
    string _enemyPhase = "-";
    string _lastPhaseTotal = "-";

    void Reset()
    {
        if (!combatDirector) combatDirector = FindFirstObjectByType<CombatDirector>(FindObjectsInactive.Include);
        if (!outputText)  outputText  = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (!combatDirector)
            combatDirector = FindFirstObjectByType<CombatDirector>(FindObjectsInactive.Include);

        if (combatDirector != null)
        {
            combatDirector.onStepChanged.AddListener(OnStepChanged);
            combatDirector.onWaitingForTargetChanged.AddListener(OnWaitingForTargetChanged);
            combatDirector.onRoundStarted.AddListener(OnRoundStarted);
            combatDirector.onRoundResolved.AddListener(OnRoundResolved);
            combatDirector.onPlayerDefLocked.AddListener(OnPlayerDefLocked);
            combatDirector.onPlayerAtkLocked.AddListener(OnPlayerAtkLocked);
            combatDirector.onTargetChanged.AddListener(OnTargetChanged);
            combatDirector.onEnemyTurnIndexChanged.AddListener(OnEnemyTurnIndexChanged);
            combatDirector.onEnemyPhaseStarted.AddListener(OnEnemyPhaseStarted);
            combatDirector.onEnemyPhaseEnded.AddListener(OnEnemyPhaseEnded);
        }

        if (outputText) outputText.richText = useRichTextColors;
        Render();
    }

    void OnDisable()
    {
        if (combatDirector != null)
        {
            combatDirector.onStepChanged.RemoveListener(OnStepChanged);
            combatDirector.onWaitingForTargetChanged.RemoveListener(OnWaitingForTargetChanged);
            combatDirector.onRoundStarted.RemoveListener(OnRoundStarted);
            combatDirector.onRoundResolved.RemoveListener(OnRoundResolved);
            combatDirector.onPlayerDefLocked.RemoveListener(OnPlayerDefLocked);
            combatDirector.onPlayerAtkLocked.RemoveListener(OnPlayerAtkLocked);
            combatDirector.onTargetChanged.RemoveListener(OnTargetChanged);
            combatDirector.onEnemyTurnIndexChanged.RemoveListener(OnEnemyTurnIndexChanged);
            combatDirector.onEnemyPhaseStarted.RemoveListener(OnEnemyPhaseStarted);
            combatDirector.onEnemyPhaseEnded.RemoveListener(OnEnemyPhaseEnded);
        }
    }

    // === Event Handlers ===
    public void OnStepChanged(TurnStep step) { _step = step; Render(); }
    public void OnWaitingForTargetChanged(bool waiting) { _waitingTarget = waiting; Render(); }

    public void OnRoundStarted()
    {
        _round = "STARTED";
        _pDef = -1; _pAtk = -1;
        _target = "-"; _enemyTurn = "-"; _enemyPhase = "-"; _lastPhaseTotal = "-";
        Render();
    }

    public void OnRoundResolved() { _round = "RESOLVED"; Render(); }
    public void OnPlayerDefLocked(int total) { _pDef = total; Render(); }
    public void OnPlayerAtkLocked(int total) { _pAtk = total; Render(); }
    public void OnTargetChanged(SimpleCombatant target) { _target = target ? target.name : "-"; Render(); }
    public void OnEnemyTurnIndexChanged(SimpleCombatant enemy, int index) { _enemyTurn = enemy ? $"{enemy.name} (#{index+1})" : "-"; Render(); }
    public void OnEnemyPhaseStarted(SimpleCombatant enemy, PhaseKind phase) { _enemyPhase = $"{phase} ({(enemy ? enemy.name : "-")})"; _lastPhaseTotal = "-"; Render(); }
    public void OnEnemyPhaseEnded(SimpleCombatant enemy, PhaseKind phase, int total) { _lastPhaseTotal = $"{total} ({(enemy ? enemy.name : "-")} {phase})"; Render(); }

    // Tek noktadan yazdırma
    void Render()
    {
        if (!outputText) return;

        string Tag(string txt, string color) => useRichTextColors ? $"<color={color}>{txt}</color>" : txt;

        string stepStr = Tag(_step.ToString(), "#FFD166");
        string waitStr = Tag(_waitingTarget ? "YES" : "NO", _waitingTarget ? "#EF476F" : "#06D6A0");
        string roundStr= Tag(_round, _round=="RESOLVED" ? "#118AB2" : "#8D99AE");
        string pDefStr = _pDef >= 0 ? _pDef.ToString() : "-";
        string pAtkStr = _pAtk >= 0 ? _pAtk.ToString() : "-";

        outputText.text =
            $"Step: {stepStr}\n" +
            $"Round: {roundStr}\n" +
            $"Waiting Target: {waitStr}\n" +
            $"P.DEF: {pDefStr}    P.ATK: {pAtkStr}\n" +
            $"Target: {_target}\n" +
            $"Enemy Turn: {_enemyTurn}\n" +
            $"Phase: {_enemyPhase}\n" +
            $"Last Total: {_lastPhaseTotal}";
    }
}
