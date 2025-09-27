// Scripts/UI/IntentIconLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Intent Icon Library")]
public class IntentIconLibrary : ScriptableObject
{
    [Serializable] public class Entry { public string key; public Sprite sprite; }

    [SerializeField] private List<Entry> entries = new();
    private Dictionary<string, Sprite> map;

    void OnEnable()
    {
        map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries) if (!string.IsNullOrEmpty(e.key) && e.sprite) map[e.key] = e.sprite;
    }

    public Sprite Get(string key)
    {
        if (string.IsNullOrEmpty(key) || map == null) return null;
        return map.TryGetValue(key, out var s) ? s : null;
    }
}
