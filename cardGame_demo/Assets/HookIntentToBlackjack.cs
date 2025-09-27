// using UnityEngine;

// // Örnek: DEF fazındayken kalkan, ATK fazında kılıç göster
// public class HookIntentToBlackjack : MonoBehaviour
// {
//     [SerializeField] private BlackjackCombatController ctrl;
//     [SerializeField] private CharacterIntentSource playerIntent;

//     private CombatPhase _phase;

//     public void OnPhaseChanged(CombatPhase p) { _phase = p; }

//     public void OnTotalsChanged(int def, int atk)
//     {
//         if (_phase == CombatPhase.DefenseDraw)
//         {
//             if (def > 0) playerIntent.SetBlock(def);
//             else playerIntent.SetBuff("Defend"); // 0 ise ikon kalsın, metin boş olabilir
//         }
//         else if (_phase == CombatPhase.AttackDraw)
//         {
//             if (atk > 0) playerIntent.SetAttack(atk, 1);
//             else playerIntent.SetBuff("Attack");
//         }
//     }
// }
