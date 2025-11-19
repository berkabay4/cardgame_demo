// using UnityEngine;

// [CreateAssetMenu(menuName = "CardGame/Enemy/Elite Enemy/Act MiniBoss DB", fileName = "ActMiniBossDb")]
// public class ActMiniBossDatabase : ScriptableObject
// {
//     [Header("Identity")]
//     [Tooltip("Bu mini boss havuzunun ait olduğu ACT.")]
//     public Act act;

//     [System.Serializable]
//     public class MiniBossEntry
//     {
//         [Tooltip("Mini boss tanımı (stats + prefab + behaviour).")]
//         public MiniBossDefinition miniBoss;

//         [Tooltip("Weighted random için ağırlık. 1 = nadir, 5 = sık, 0 = hiç çıkmaz.")]
//         public int weight = 1;
//     }

//     [Header("Mini Boss Havuzu")]
//     public MiniBossEntry[] miniBosses;

//     /// <summary>Bu ACT için weighted random bir mini boss döner.</summary>
//     public MiniBossDefinition GetRandomMiniBoss(System.Random rng)
//     {
//         if (miniBosses == null || miniBosses.Length == 0)
//             return null;

//         int totalWeight = 0;
//         foreach (var entry in miniBosses)
//         {
//             if (entry == null || entry.miniBoss == null) continue;
//             if (entry.weight <= 0) continue;
//             totalWeight += entry.weight;
//         }

//         if (totalWeight <= 0)
//             return null;

//         int roll = rng.Next(0, totalWeight);
//         int cumulative = 0;

//         foreach (var entry in miniBosses)
//         {
//             if (entry == null || entry.miniBoss == null) continue;
//             if (entry.weight <= 0) continue;

//             cumulative += entry.weight;
//             if (roll < cumulative)
//             {
//                 return entry.miniBoss;
//             }
//         }

//         return null;
//     }
// }
