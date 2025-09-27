// Scripts/Combat/CharacterIntentSource.cs
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class CharacterIntentSource : MonoBehaviour, IIntentSource
{
    [Header("Anchor")]
    [SerializeField] private Transform worldAnchor;

    [Header("Intent (Debug/Example)")]
    [SerializeField] private string intentKey = "Attack"; // Attack/Defend/Buff...
    [SerializeField, Min(0)] private int attackPerHit = 8;
    [SerializeField, Min(1)] private int attackHits = 1;
    [SerializeField, Min(0)] private int blockGain = 0;

    public event Action OnIntentChanged;

    public int AttackPerHit => attackPerHit;
    public int AttackHits   => attackHits;
    public int BlockGain    => blockGain;
    public string IntentKey => intentKey;
    public Transform WorldAnchor => worldAnchor ? worldAnchor : transform;

    // Oyun akışında niyet değiştiğinde bu metodu çağır
    public void SetAttack(int perHit, int hits)
    {
        attackPerHit = Mathf.Max(0, perHit);
        attackHits   = Mathf.Max(1, hits);
        blockGain    = 0;
        intentKey    = "Attack";
        OnIntentChanged?.Invoke();
    }

    public void SetBlock(int amount)
    {
        blockGain    = Mathf.Max(0, amount);
        attackPerHit = 0;
        attackHits   = 1;
        intentKey    = "Defend";
        OnIntentChanged?.Invoke();
    }

    public void SetBuff(string key = "Buff")
    {
        blockGain = 0;
        attackPerHit = 0;
        attackHits = 1;
        intentKey = key;
        OnIntentChanged?.Invoke();
    }
}
