using System;
using System.Collections.Generic;
using UnityEngine;

public enum Suit { Clubs, Diamonds, Hearts, Spades }
public enum Rank { Ace = 1, Two=2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack=11, Queen=12, King=13 }

[Serializable]
public struct Card
{
    public Suit Suit;
    public Rank Rank;
    public Card(Suit s, Rank r) { Suit = s; Rank = r; }

    public int PrimaryValue => Rank switch
    {
        Rank.Jack or Rank.Queen or Rank.King => 10,
        Rank.Ace => 11,
        _ => (int)Rank
    };

    public override string ToString() => $"{Rank} of {Suit}";
}

// public static class BlackjackMath
// {
//     public static int BestTotal(IList<Card> cards, int threshold)
//     {
//         int total = 0;
//         int aceCount = 0;
//         foreach (var c in cards)
//         {
//             total += c.PrimaryValue;
//             if (c.Rank == Rank.Ace) aceCount++;
//         }
//         while (total > threshold && aceCount > 0)
//         {
//             total -= 10; // Ace 11 -> 1
//             aceCount--;
//         }
//         return total;
//     }
// }
