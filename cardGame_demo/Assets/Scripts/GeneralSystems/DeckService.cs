// DeckService.cs
using System.Collections.Generic;
using UnityEngine;


public interface IDeckService
{
    int Count { get; }

    bool AddCard(Card card);
    bool AddJoker();
    void Clear();
    void Shuffle(int? seed = null);

    // Daha önce ekledik:
    void RebuildAndShuffle();

    // <<< YENİ >>>
    /// <summary>Üstten 1 kart çeker; kart yoksa null döner.</summary>
    Card Draw();

    // (Opsiyonel, istersen kullan)
    bool TryDraw(out Card card);
}

public class DeckService : IDeckService
{
    // Çekilecek deste
    private readonly List<Card> _drawPile = new();
    // Atılan/harcanan kartlar
    private readonly List<Card> _discardPile = new();
    // Baştaki “temel deste” anlık görüntüsü (rebuild için)
    private List<Card> _snapshot;

    private System.Random _rng;
    private readonly int _capacity; // 0 = sınırsız

    public int Count => _drawPile.Count;
    public IReadOnlyList<Card> Cards => _drawPile; // istersen expose etme

    public DeckService(int? seed = null, int capacity = 0)
    {
        _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        _capacity = capacity;
    }

    // Başlangıç kartlarını vermek istersen (ör. sahne kurulurken)
    public void SetInitialCards(IEnumerable<Card> cards, bool takeSnapshot = true)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        if (cards != null)
        {
            _drawPile.AddRange(cards);
        }
        if (takeSnapshot)
        {
            _snapshot = new List<Card>(_drawPile);
        }
    }

    public bool AddCard(Card card)
    {
        if (card == null) return false;
        if (_capacity > 0 && _drawPile.Count >= _capacity) return false;
        _drawPile.Add(card); // alta ekliyoruz (istersen üste de koyabilirsin)
        return true;
    }

    public bool AddJoker()
    {
        var joker = new Card(Rank.Joker, "None");
        return AddCard(joker);
    }

    public void Clear()
    {
        _drawPile.Clear();
        _discardPile.Clear();
    }
    public Card Draw()
    {
        return DrawTop(); // zaten yazmıştık
    }

    public bool TryDraw(out Card card)
    {
        card = DrawTop();
        return card != null;
    }
    public void Shuffle(int? seed = null)
    {
        if (seed.HasValue) _rng = new System.Random(seed.Value);
        for (int i = _drawPile.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
        }
    }

    /// <summary>
    /// Çekilecek kart kalmadığında: önce discard'ı draw'a bas, o da yoksa snapshot'tan yeniden kur,
    /// sonra karıştır.
    /// </summary>
    public void RebuildAndShuffle()
    {
        if (_drawPile.Count > 0) return;

        if (_discardPile.Count > 0)
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle();
            return;
        }

        if (_snapshot != null && _snapshot.Count > 0)
        {
            _drawPile.Clear();
            _drawPile.AddRange(_snapshot);
            Shuffle();
        }
        // Aksi halde gerçekten destede hiç kart yok → Count 0 kalır
    }

    // ==== İsteğe bağlı yardımcılar (acc.Hit içinden çağrılabilir) ====

    /// <summary>Üstten 1 kart çek (yoksa null döner).</summary>
    public Card DrawTop()
    {
        if (_drawPile.Count == 0) return null;
        int last = _drawPile.Count - 1;
        var c = _drawPile[last];
        _drawPile.RemoveAt(last);
        return c;
    }

    /// <summary>Çekilen/oynanan kartı iskartaya at.</summary>
    public void Discard(Card c)
    {
        if (c != null) _discardPile.Add(c);
    }

    /// <summary>Birden çok kartı iskartaya atmak için.</summary>
    public void DiscardMany(IEnumerable<Card> cards)
    {
        if (cards == null) return;
        _discardPile.AddRange(cards);
    }
}