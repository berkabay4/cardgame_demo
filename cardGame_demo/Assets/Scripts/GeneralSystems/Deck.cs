// DeckService.cs
using System.Collections.Generic;
using UnityEngine;

public interface IDeckService
{
    int Count { get; }
    Card Draw();
    void RebuildAndShuffle();
}

public class DeckService : IDeckService
{
    private readonly List<Card> _cards = new();
    private readonly System.Random _rng;

    // İsteğe bağlı: Joker sayısı sabiti (2)
    private const int JokerCount = 2;

    public int Count => _cards.Count;

    public DeckService(int? seed = null)
    {
        _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        RebuildAndShuffle();
    }
    public void ClearAndAdd(IEnumerable<Card> cards)
    {
        _cards.Clear();
        if (cards != null) _cards.AddRange(cards);
    }
    public void RebuildAndShuffle()
    {
        _cards.Clear();

        // --- 52 normal kart ---
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
        {
            if (s.Equals(Suit.None)) continue; // Joker için ayrılmış koz yoksa bunu kaldır

            foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
            {
                if (r.Equals(Rank.Joker)) continue; // Normal dağılıma Joker'i katma
                _cards.Add(new Card(r,s.ToString()));
            }
        }

        // --- +2 Joker ---
        for (int i = 0; i < JokerCount; i++)
        {
            _cards.Add(new Card(Rank.Joker, Suit.None.ToString()));
        }

        // Fisher–Yates
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }

        // Debug.Log($"[Deck] Kuruldu+karıştırıldı ({_cards.Count})"); // 54 olmalı
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            Debug.Log("[Deck] Bitti → yeniden kuruluyor.");
            RebuildAndShuffle();
             Debug.Log("sa5");
        }

        var c = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        // Debug.Log($"[Deck] Çekildi: {c}  (Kalan:{_cards.Count})");
        return c;
    }
}
