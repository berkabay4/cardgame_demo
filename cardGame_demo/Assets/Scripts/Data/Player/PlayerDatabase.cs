using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Player/Player Database", fileName = "PlayerDatabase")]
public class PlayerDatabase : ScriptableObject
{
    public List<PlayerData> all = new List<PlayerData>();
}
