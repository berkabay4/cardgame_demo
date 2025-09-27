// BlackjackMath.cs
using System.Collections.Generic;

public static class BlackjackMath
{
    // DÜZ TOPLAM: Ace'ler asla 1'e düşmez; toplam threshold'u aşarsa bust olur.
    public static int RawTotal(IList<Card> cards)
    {
        int total = 0;
        foreach (var c in cards) total += c.PrimaryValue; // A=11, J/Q/K=10, diğerleri pip
        return total;
    }
}
