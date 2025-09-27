// Scripts/Combat/IIntentSource.cs
using UnityEngine;
using System;

public interface IIntentSource
{
    int AttackPerHit { get; }
    int AttackHits { get; }
    int BlockGain { get; }
    string IntentKey { get; }      // "Attack" | "Defend" | "Buff" | ...
    Transform WorldAnchor { get; } // Rozetin takip edeceği dünya noktası

    event Action OnIntentChanged;
}
