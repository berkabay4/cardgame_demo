// // Scripts/UI/IntentBadgeView.cs
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;

// public class IntentBadgeView : MonoBehaviour
// {
//     [SerializeField] private Image icon;
//     [SerializeField] private TextMeshProUGUI text;

//     public void SetData(Sprite sprite, int atkPerHit, int hits, int block)
//     {
//         if (icon) icon.sprite = sprite;

//         if (atkPerHit > 0)
//         {
//             text.SetText(hits > 1 ? "⚔ {0}×{1}" : "⚔ {0}", atkPerHit, hits);
//         }
//         else if (block > 0)
//         {
//             text.SetText("🛡 {0}", block);
//         }
//         else
//         {
//             text.SetText(""); // sadece ikon (Buff/Debuff)
//         }
//     }
// }
