using System;
using System.Collections.Generic;
using UnityEngine;

public enum Suit { None, Clubs, Diamonds, Hearts, Spades }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack = 10, Queen = 10, King = 10, Ace = 11, Joker = 0 }

[Serializable]
    public sealed class Card
    {
        public Rank Rank { get; }
        public int PrimaryValue { get; }  // Ace=11, J/Q/K=10, pip=2..10; Joker için 0 tutabiliriz
        public string Suit { get; }       // Joker için "None" olabilir

        public bool IsJoker => Rank == Rank.Joker;

        public Card(Rank rank, string suit)
        {
            Rank = rank;
            Suit = suit;
            PrimaryValue = rank == Rank.Ace ? 11 :
                        rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King ? 10 :
                        rank == Rank.Joker ? 0 :
                        (int)rank;
        }

        public override string ToString() => IsJoker ? "JOKER" : $"{Rank} of {Suit}";
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
