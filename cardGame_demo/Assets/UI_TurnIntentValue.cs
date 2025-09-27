// // TMPProgressText.cs  (güncel)
// using TMPro;
// using UnityEngine;

// [DisallowMultipleComponent]
// public class UI_TurnIntentValue : MonoBehaviour
// {
//     [SerializeField] private TextMeshProUGUI target;   // Hem TextMeshProUGUI hem TextMeshPro desteği
//     [SerializeField] private string format = "{0} / {1}";

//     void Reset()
//     {
//         if (!target) target = GetComponent<TextMeshProUGUI>();
//     }

//     void Awake()
//     {
//         if (!target) target = GetComponent<TextMeshProUGUI>();
//         if (!target) Debug.LogWarning($"[TMPProgressText] '{name}' üzerinde TMP_Text bulunamadı.");
//     }

//     // UnityEvent<int,int> ile bağlanacak
//     public void SetProgress(int current, int max)
//     {
//         if (!target)
//         {
//             Debug.LogWarning($"[TMPProgressText] target yok. '{name}'");
//             return;
//         }
//         target.SetText(format, current, max);
//         // Teşhis için:
//         // Debug.Log($"[TMPProgressText] {name} -> {current} / {max}");
//     }
// }
