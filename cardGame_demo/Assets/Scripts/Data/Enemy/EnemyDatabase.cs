using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Database", fileName = "EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyData> all = new List<EnemyData>();
}
