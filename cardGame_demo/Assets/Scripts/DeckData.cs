using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Deck/DeckData", fileName = "DeckData")]
public class DeckData : ScriptableObject
{
    [Header("Base")]
    [Tooltip("Klasik 52'lik deste eklensin mi?")]
    public bool includeStandard52 = true;

    [Tooltip("Eklenmesini istediğin Joker sayısı")]
    [Min(0)] public int extraJokers = 0;

    [Header("Extras")]
    [Tooltip("Klasik 52 harici eklenecek kartlar (ör. özel kartlar).")]
    public List<Card> extraCards = new();

    /// <summary>Bu DeckData'nın birleşik kart listesini üretir.</summary>
    public List<Card> GetCards()
    {
        var list = new List<Card>(64);

        if (includeStandard52)
            list.AddRange(CreateStandard52());

        if (extraJokers > 0)
        {
            for (int i = 0; i < extraJokers; i++)
                list.Add(new Card(Rank.Joker, "None"));
        }

        if (extraCards != null && extraCards.Count > 0)
            list.AddRange(extraCards);

        return list;
    }

    // --- Helpers ---
    public static List<Card> CreateStandard52()
    {
        var list = new List<Card>(52);
        string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades" };

        foreach (var s in suits)
        {
            for (int v = 2; v <= 10; v++) list.Add(new Card((Rank)v, s));
            list.Add(new Card(Rank.Jack,  s));
            list.Add(new Card(Rank.Queen, s));
            list.Add(new Card(Rank.King,  s));
            list.Add(new Card(Rank.Ace,   s));
        }
        return list;
    }
}
