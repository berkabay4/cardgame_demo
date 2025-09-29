using System.Collections.Generic;
using UnityEngine;

public class PhaseAccumulator
{
    public readonly List<Card> Cards = new();
    public bool IsStanding { get; private set; }
    public bool IsBusted  { get; private set; }
    public int Total      { get; private set; }

    readonly string _name;
    readonly bool _isPlayer; // <<< EKLENDİ

    public PhaseAccumulator(string name, bool isPlayer = false)
    {
        _name = name;
        _isPlayer = isPlayer;
    }

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

        // --- JOKER KURALI (Sadece OYUNCU için) ---
        if (_isPlayer && c.IsJoker)
        {
            Total = threshold;
            IsStanding = true;         // otomatik Stand
            // Debug.Log($"[{_name}] HIT → JOKER! {Total}/{threshold} (auto-Stand)");
            return;
        }

        int raw = BlackjackMath.RawTotal(Cards);
        Total = raw;

        // Debug.Log($"[{_name}] HIT → {c} | {Total}/{threshold}");

        if (Total > threshold)
        {
            IsBusted = true;
            Total = 0;
            // Debug.Log($"[{_name}] BUST! Value becomes 0.");
        }
    }

    public void Stand(int threshold)
    {
        if (IsBusted) return;
        Total = BlackjackMath.RawTotal(Cards);
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
