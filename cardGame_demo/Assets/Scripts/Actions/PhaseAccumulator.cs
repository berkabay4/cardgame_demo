using System.Collections.Generic;
using UnityEngine;

public class PhaseAccumulator
{
    public readonly List<Card> Cards = new();
    public bool IsStanding { get; private set; }
    public bool IsBusted  { get; private set; }
    public int Total      { get; private set; }

    readonly string _name;
    public PhaseAccumulator(string name){ _name = name; }

    public void Reset()
    {
        Cards.Clear();
        IsStanding = false;
        IsBusted = false;
        Total = 0;
        Debug.Log($"[{_name}] Reset");
    }

    public void Hit(IDeckService deck, int threshold)
    {
        if (IsStanding || IsBusted) return;

        var c = deck.Draw();
        Cards.Add(c);

        int raw = BlackjackMath.RawTotal(Cards); // ← düz toplam
        Total = raw;

        Debug.Log($"[{_name}] HIT → {c} | {Total}/{threshold}");

        if (Total > threshold)
        {
            IsBusted = true;
            Total = 0; // kural: eşik aşıldı → değer 0
            Debug.Log($"[{_name}] BUST! Value becomes 0.");
        }
    }

    public void Stand(int threshold)
    {
        if (IsBusted) return;
        Total = BlackjackMath.RawTotal(Cards); // ← düz toplam
        IsStanding = true;
        Debug.Log($"[{_name}] STAND → {Total}");
        if (Total > threshold)
        {
            IsBusted = true;
            Total = 0;
            Debug.Log($"[{_name}] BUST on Stand! Value becomes 0.");
        }
    }
}
