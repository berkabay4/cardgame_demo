using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class CombatantDeck : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Bu aktör için kullanılacak DeckData (başlangıç)")]
    public DeckData deckData;

    [Header("Runtime Additions")]
    [Tooltip("Oyun sırasında eklenen kartlar (örn. RestSite → Joker).")]
    [SerializeField] private List<Card> additionalDeck = new();   // ← yeni

    [Header("Start Options")]
    [Tooltip("Sahne başında karılsın mı?")]
    public bool shuffleOnStart = true;

    // ---- PUBLIC API (RestSite vb. kullanır) ----
    public void AddToAdditional(Card card)
    {
        if (card == null) return;
        additionalDeck.Add(card);
    }

    public void AddJokerToAdditional(int count = 1)
    {
        if (count < 1) return;
        for (int i = 0; i < count; i++)
            additionalDeck.Add(new Card(Rank.Joker, "None"));
    }

    public IReadOnlyList<Card> AdditionalDeck => additionalDeck;

    public void ClearAdditional()
    {
        additionalDeck.Clear();
    }

    // Birleşik final liste
    public List<Card> GetFinalCards()
    {
        var list = new List<Card>(64);

        // 1) Base
        List<Card> baseCards = null;
        if (deckData != null)
            baseCards = deckData.GetCards();

        if (baseCards == null || baseCards.Count == 0)
            baseCards = DeckData.CreateStandard52(); // güvenli fallback

        list.AddRange(baseCards);

        // 2) Runtime additions
        if (additionalDeck != null && additionalDeck.Count > 0)
            list.AddRange(additionalDeck);

        return list;
    }

    public IDeckService BuildDeck()
    {
        var deck = new DeckService();

        var finalCards = GetFinalCards();                          // base + additional
        deck.SetInitialCards(finalCards, takeSnapshot: true);       // snapshot → rebuild’ler çalışır

        if (shuffleOnStart)
            deck.Shuffle();

        Debug.Log($"[CombatantDeck] Built from {(deckData ? deckData.name : "Default")} + Additional({additionalDeck?.Count ?? 0}) → {deck.Count} cards");
        return deck;
    }
}
