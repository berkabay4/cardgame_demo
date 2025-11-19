// RunContext.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/RunContext")]
public class RunContext : ScriptableObject
{
    public enum EncounterTier { Minor, Elite , Boss }
    [System.Serializable]
    public class EncounterData {
        public Map.MapNode mapNode;
        public Map.NodeType nodeType;
        public string blueprintName;
        public EncounterTier tier = EncounterTier.Minor;
        public int seed;
    }

    [System.Serializable]
    public class CombatResult {
        public bool playerWon;
        public int coins;
    }
    [Header("Run State")]
    public Act currentAct = Act.Act1;
    public EncounterData pendingEncounter;
    public CombatResult lastCombatResult;
    public int pendingCoins;

    public void ClearAll() {
        pendingEncounter = null;
        lastCombatResult = null;
        pendingCoins = 0;
    }
}
