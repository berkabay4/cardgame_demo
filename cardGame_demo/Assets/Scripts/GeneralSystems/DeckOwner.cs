using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DeckOwner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField, Min(0)] private int capacity = 0; // 0 = sınırsız
    [SerializeField] private bool createOnAwake = true;
    [SerializeField] private int? seedForShuffle = null;

    private DeckService _deck;
    public IDeckService Deck => _deck;

    [System.Serializable] public class CardEvent : UnityEvent<Card> {}
    [System.Serializable] public class CountEvent : UnityEvent<int> {}

    public CardEvent onCardAdded;
    public CountEvent onCountChanged;
    public UnityEvent onDeckReset;

    void Awake()
    {
        if (createOnAwake)
            CreateNewDeck(seedForShuffle);
    }

    public void CreateNewDeck(int? seed = null)
    {
        _deck = new DeckService(seed, capacity);
        onDeckReset?.Invoke();
        onCountChanged?.Invoke(_deck.Count);
    }

    public void SetDeck(DeckService deck)
    {
        _deck = deck;
        onDeckReset?.Invoke();
        onCountChanged?.Invoke(_deck?.Count ?? 0);
    }

    public void EnsureDeck(int? seed = null)
    {
        if (_deck == null) CreateNewDeck(seed);
    }

    // ---- Kolay erişimler ----
    public bool AddCard(Card card)
    {
        EnsureDeck();
        bool ok = _deck.AddCard(card);
        if (ok)
        {
            onCardAdded?.Invoke(card);
            onCountChanged?.Invoke(_deck.Count);
        }
        return ok;
    }

    public bool AddJoker()
    {
        EnsureDeck();
        bool ok = _deck.AddJoker();
        if (ok)
        {
            // Eklenen kartı event’e geçirmek istersen son elemanı kullanabilirsin
            var added = _deck.Cards[_deck.Count - 1];
            onCardAdded?.Invoke(added);
            onCountChanged?.Invoke(_deck.Count);
        }
        return ok;
    }

    public void Shuffle(int? seed = null)
    {
        if (_deck == null) return;
        _deck.Shuffle(seed);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (capacity < 0) capacity = 0;
    }
#endif
}
