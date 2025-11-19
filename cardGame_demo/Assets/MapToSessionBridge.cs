using UnityEngine;

public class MapToSessionBridge : MonoBehaviour
{
    [SerializeField] Map.MapPlayerTracker tracker;

    void Awake() { if (!tracker) tracker = Map.MapPlayerTracker.Instance; }
    void OnEnable()
    {
        if (!tracker) return;
        tracker.onNodeEntered.AddListener(HandleNodeEntered);   // geçişe hazır olduğunda
        // İstersen tracker.onNodeSelected.AddListener(HandleNodeSelected);
    }
    void OnDisable()
    {
        if (!tracker) return;
        tracker.onNodeEntered.RemoveListener(HandleNodeEntered);
        // tracker.onNodeSelected.RemoveListener(HandleNodeSelected);
    }

    void HandleNodeEntered(Map.MapNode node)
    {
        if (!node) return;

        switch (node.Node.nodeType)
        {
            case Map.NodeType.MinorEnemy:
                GameSessionDirector.Instance.StartMinorEncounter(node);
                break;
            case Map.NodeType.EliteEnemy:
                GameSessionDirector.Instance.StartMinorEncounter(node);
                break;
            case Map.NodeType.Treasure:
                GameSessionDirector.Instance.StartTreasure(node);
                break;

            case Map.NodeType.RestSite:
                GameSessionDirector.Instance.StartRestSite(node);
                break;
            case Map.NodeType.Mystery:
                GameSessionDirector.Instance.StartMystery(node);
                break;

            default:
                Debug.Log($"[Bridge] Non-wired node: {node.Node.nodeType}");
                break;
        }
    }
}
