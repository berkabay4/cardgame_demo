// RunContext.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/RunContext")]
public class RunContext : ScriptableObject
{
    [System.Serializable]
    public class EncounterData {
        public Map.MapNode mapNode;
        public Map.NodeType nodeType;
        public string blueprintName;
        public int seed;
    }

    [System.Serializable]
    public class CombatResult {
        public bool playerWon;
        public int coins;
    }

    public EncounterData pendingEncounter;
    public CombatResult lastCombatResult;
    public int pendingCoins;

    public void ClearAll() {
        pendingEncounter = null;
        lastCombatResult = null;
        pendingCoins = 0;
    }
}
