using UnityEngine;
using System.Collections.Generic;

public enum RelicRarity { Common, Uncommon, Rare, Boss, Event }
public enum RelicStackRule { Unique, Stackable, ReplaceLower, ReplaceHigher }

[CreateAssetMenu(menuName = "CardGame/Relic Definition")]
public class RelicDefinition : ScriptableObject
{
    public string relicId;                     // "relic_bloody_idol"
    public string displayName;
    [TextArea] public string descriptionTemplate; // "{0}% daha fazla hasar. Her tur +{1} enerji."
    public Sprite icon;
    public RelicRarity rarity = RelicRarity.Common;

    [Header("Stacking")]
    public RelicStackRule stackRule = RelicStackRule.Stackable;
    [Min(1)] public int maxStacks = 99;

    [Header("Effects")]
    [SerializeReference] public List<IRelicEffect> effects = new(); 
    // [SerializeReference] için: effect sınıflarını [System.Serializable] yapacağız.
}
