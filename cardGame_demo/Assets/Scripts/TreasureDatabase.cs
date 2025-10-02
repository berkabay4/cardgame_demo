// TreasureDatabase.cs
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Game/Treasure/TreasureDatabase")]
public class TreasureDatabase : ScriptableObject
{
    [System.Serializable]
    public class ActEntry
    {
        public Act act = Act.Act1;
        public ActRelicDatabase relics; // Act’e özel RelicDefinition listesi
        public ActCoinDatabase  coins;  // Act’e özel coin aralığı
    }

    [Tooltip("Her act için coin ve relic DB eşlemesi")]
    public ActEntry[] acts;

    public ActRelicDatabase GetRelicDB(Act act)
    {
        if (acts == null || acts.Length == 0) return null;
        var e = acts.FirstOrDefault(x => x != null && x.act == act);
        return (e != null) ? e.relics : null;   // ✅ e != null
    }

    public ActCoinDatabase GetCoinDB(Act act)
    {
        if (acts == null || acts.Length == 0) return null;
        var e = acts.FirstOrDefault(x => x != null && x.act == act);
        return (e != null) ? e.coins : null;    // ✅ e != null
    }

    // (Opsiyonel) Güvenli sürümler:
    public bool TryGetRelicDB(Act act, out ActRelicDatabase db)
    {
        db = GetRelicDB(act);
        return db != null;
    }

    public bool TryGetCoinDB(Act act, out ActCoinDatabase db)
    {
        db = GetCoinDB(act);
        return db != null;
    }
}
