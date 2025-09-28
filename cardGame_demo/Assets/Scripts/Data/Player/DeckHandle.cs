using UnityEngine;

[DisallowMultipleComponent]
public class DeckHandle : MonoBehaviour
{
    public IDeckService Deck { get; private set; }
    public void Bind(IDeckService deck) => Deck = deck;
}
