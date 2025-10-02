using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Treasure/ActRelicDatabase")]
public class ActRelicDatabase : ScriptableObject
{
    public Act act = Act.Act1;

    [Tooltip("Bu act için verilebilecek relic asset referansları")]
    public List<RelicDefinition> relics = new();

    public bool TryGetRandomRelic(System.Random rng, out RelicDefinition def)
    {
        def = null;
        if (relics == null || relics.Count == 0) return false;
        def = relics[rng.Next(0, relics.Count)];
        return def != null;
    }
}
