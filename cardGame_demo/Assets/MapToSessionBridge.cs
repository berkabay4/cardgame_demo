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

            // Diğer tipler:
            // case Map.NodeType.RestSite:  OpenRest(node);  break;
            // case Map.NodeType.Store:     OpenShop(node);  break;
            default:
                Debug.Log($"[Bridge] Non-combat node entered: {node.Node.nodeType}");
                // GUI tabanlı akışsa bittiğinde tracker.Unlock() çağır.
                break;
        }
    }
}
