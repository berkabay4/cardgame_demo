using UnityEngine;

[DisallowMultipleComponent]
public class DeckOwner : MonoBehaviour
{
    // Her aktöre özel runtime deste (paylaşımsız)
    private DeckService _deck;
    public IDeckService Deck => _deck;

    /// <summary>Yeni bir deste oluşturur. İsteğe bağlı seed ile deterministik yapılabilir.</summary>
    public void CreateNewDeck(int? seed = null)
    {
        _deck = new DeckService(seed);
    }

    /// <summary>Var olan deste örneğini enjekte etmek istersen.</summary>
    public void SetDeck(DeckService deck)
    {
        _deck = deck;
    }
}
