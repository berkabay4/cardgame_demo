using System;
using UnityEngine;

public enum Suit { None, Clubs, Diamonds, Hearts, Spades }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack = 10, Queen = 10, King = 10, Ace = 11, Joker = 0 }

[Serializable]
public sealed class Card
{
    [SerializeField] private Rank rank;
    [SerializeField] private string suit = "None";   // string’te kalalım istedin
    [SerializeField] private int primaryValue;

    public Rank Rank => rank;
    public string Suit => suit;
    public int PrimaryValue => primaryValue;

    public bool IsJoker => rank == Rank.Joker;

    public Card(Rank rank, string suit)
    {
        this.rank = rank;
        this.suit = suit;
        primaryValue = rank == Rank.Ace ? 11 :
                       (rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King) ? 10 :
                       (rank == Rank.Joker ? 0 : (int)rank);
        if (IsJoker) this.suit = "None";
    }

    // Unity için parametresiz ctor
    public Card() { }

    public override string ToString() => IsJoker ? "JOKER" : $"{Rank} of {Suit}";
}
