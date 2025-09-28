using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyRegistry
{
    readonly GameDirector _host;
    readonly SimpleCombatant _player;
    readonly UnityEngine.Events.UnityEvent<string> _log;

    public List<SimpleCombatant> All { get; private set; } = new();
    public List<SimpleCombatant> AliveEnemies => All.Where(e => e && e.CurrentHP > 0).ToList();

    public EnemyRegistry(GameDirector host, SimpleCombatant player, List<SimpleCombatant> initial,
                         UnityEngine.Events.UnityEvent<string> log)
    {
        _host = host; _player = player; _log = log;
        All = initial ?? new List<SimpleCombatant>();
        Refresh(); // normalize ordering & context enemy
    }

    public void Refresh()
    {
        var all = Object.FindObjectsByType<SimpleCombatant>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var list = new List<SimpleCombatant>();
        foreach (var sc in all)
        {
            if (!sc) continue;
            if (_player && sc == _player) continue;
            if (sc.CurrentHP <= 0) continue;
            list.Add(sc);
        }
        list.Sort((a,b) => GetSpawnOrder(a).CompareTo(GetSpawnOrder(b)));
        All = list;

        var first = All.Count > 0 ? All[0] : null;
        if (first != null)
            _host.Ctx.SetEnemy(first);

        foreach (var sc in All)
        {
            if (!sc) continue;
            // Deck kayıtlı değilse ekle
            var ctx = _host.Ctx;
            if (!ctx.DecksByUnit.ContainsKey(sc))
            {
                var deck = _host.GetType()
                    .GetMethod("BuildDeckForUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(_host, new object[]{ sc }) as IDeckService;

                ctx.RegisterDeck(sc, deck);
            }
        }
        _log?.Invoke($"[Enemies] Refreshed (by spawn index). Count={All.Count}");
    }

    int GetSpawnOrder(SimpleCombatant sc)
    {
        if (!sc) return int.MaxValue;
        var meta = sc.GetComponent<EnemySpawnMeta>();
        if (meta && meta.spawnIndex >= 0) return meta.spawnIndex;
        return int.MaxValue;
    }
}
