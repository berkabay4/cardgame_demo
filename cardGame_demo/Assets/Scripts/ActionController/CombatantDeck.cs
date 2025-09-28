// CombatantDeck.cs
using UnityEngine;
using System.Collections.Generic;
using SingularityGroup.HotReload;

[DisallowMultipleComponent]
public class CombatantDeck : MonoBehaviour
{
    [Tooltip("Bu aktörün destesi için başlangıç kartları (boşsa fabrika default üretir).")]
    public List<Card> initialCards = new();

    [Tooltip("Sahne başında karılsın mı?")]
    public bool shuffleOnStart = true;

    public IDeckService BuildDeck()
    {
        var deck = new DeckService();  // Kendi DeckService'ini kullan
        if (initialCards != null && initialCards.Count > 0)
        {
            deck.ClearAndAdd(initialCards); // Eğer yoksa: önce deck.Clear(); sonra tek tek deck.Add(card);
        }
        if (shuffleOnStart)
        {
            deck.RebuildAndShuffle();
            Debug.Log("sa1");
        }
        return deck;
    }
}
