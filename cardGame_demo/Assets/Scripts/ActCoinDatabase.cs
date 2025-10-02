using UnityEngine;

[CreateAssetMenu(menuName="Game/Treasure/ActCoinDatabase")]
public class ActCoinDatabase : ScriptableObject
{
    public Act act = Act.Act1;                   // ✅ int yerine enum
    public Vector2Int coinRange = new Vector2Int(50, 100);

    public int RollCoins(System.Random rng)
    {
        int min = Mathf.Min(coinRange.x, coinRange.y);
        int max = Mathf.Max(coinRange.x, coinRange.y) + 1; // üst dahil
        return rng.Next(min, max);
    }
}
