using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class CombatantDeck : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Bu aktör için kullanılacak DeckData")]
    public DeckData deckData;

    [Header("Start Options")]
    [Tooltip("Sahne başında karılsın mı?")]
    public bool shuffleOnStart = true;

    public IDeckService BuildDeck()
    {
        var deck = new DeckService();

        List<Card> cards = null;

        if (deckData != null)
        {
            cards = deckData.GetCards();
        }

        // DeckData yoksa tamamen boş kalmasın diye güvenli fallback
        if (cards == null || cards.Count == 0)
        {
            cards = DeckData.CreateStandard52();   // 52 default
            // İstersen buraya 1 Joker varsayılanı da ekleyebilirsin:
            // cards.Add(new Card(Rank.Joker, "None"));
        }

        deck.SetInitialCards(cards, takeSnapshot: true);

        if (shuffleOnStart)
            deck.Shuffle();

        Debug.Log($"[CombatantDeck] Built from {(deckData ? deckData.name : "Default")} → {deck.Count} cards");
        return deck;
    }
}
