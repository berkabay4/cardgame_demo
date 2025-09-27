// using UnityEngine;
// // En üste ekle:
// using UnityEngine.Events;


// public enum CombatPhase { DefenseDraw, AttackDraw, Resolve }

// public class BlackjackCombatController : MonoBehaviour
// {
//     [Header("Settings")]
//     [SerializeField, Min(1)] private int threshold = 21;
//     [SerializeField] private bool reshuffleWhenLow = true;
//     [SerializeField, Min(5)] private int lowDeckCount = 8;

//     [Header("Refs")]
//     [SerializeField] private SimpleCombatant player;
//     [SerializeField] private SimpleCombatant enemy;

//     [Header("Controls (optional)")]
//     [SerializeField] private bool enableKeyboardShortcuts = true; // H:Hit, J:Stand, N:NewTurn(after Resolve)

//     private DeckService deck;
//     private CombatPhase phase;
//     private PhaseAccumulator defAcc = new("DEF");
//     private PhaseAccumulator atkAcc = new("ATK");

//     [Header("Events (Draw Progress)")]
//     public UnityEvent<int,int> onDefProgress; // (current, max=threshold)
//     public UnityEvent<int,int> onAtkProgress; // (current, max=threshold)

//     void Awake()
//     {
//         deck = new DeckService();
//         StartNewTurn();
//     }

//     void Update()
//     {
//         if (!enableKeyboardShortcuts) return;

//         if (phase == CombatPhase.DefenseDraw || phase == CombatPhase.AttackDraw)
//         {
//             if (Input.GetKeyDown(KeyCode.H)) OnHitClicked();
//             if (Input.GetKeyDown(KeyCode.J)) OnStandClicked();
//         }
//         else if (phase == CombatPhase.Resolve)
//         {
//             if (Input.GetKeyDown(KeyCode.N)) StartNewTurn();
//         }
//     }

//     public void OnHitClicked()
//     {
//         if (phase == CombatPhase.DefenseDraw)
//         {
//             defAcc.Hit(deck, threshold);
//             Debug.Log($"[Event] DEF progress raise: {defAcc.Total}/{threshold}");
//             onDefProgress?.Invoke(defAcc.Total, threshold);
//             AnnounceTotals();
//             if (defAcc.IsBusted) NextPhase();
//         }
//         else if (phase == CombatPhase.AttackDraw)
//         {
//             atkAcc.Hit(deck, threshold);
//             Debug.Log($"[Event] ATK progress raise: {atkAcc.Total}/{threshold}");
//             onAtkProgress?.Invoke(atkAcc.Total, threshold);
//             AnnounceTotals();
//             if (atkAcc.IsBusted) NextPhase();
//         }
//     }

//     public void OnStandClicked()
//     {
//         if (phase == CombatPhase.DefenseDraw)
//         {
//             defAcc.Stand(threshold);
//             // Stand sonrası da güncel değeri yazdır (istersen)
//             onDefProgress?.Invoke(defAcc.Total, threshold);

//             AnnounceTotals();
//             NextPhase();
//         }
//         else if (phase == CombatPhase.AttackDraw)
//         {
//             atkAcc.Stand(threshold);
//             onAtkProgress?.Invoke(atkAcc.Total, threshold);

//             AnnounceTotals();
//             NextPhase();
//         }
//     }

//     // === Flow ===
//     private void StartNewTurn()
//     {
//         defAcc = new PhaseAccumulator("DEF");
//         atkAcc = new PhaseAccumulator("ATK");
//         defAcc.Reset();
//         atkAcc.Reset();

//         if (reshuffleWhenLow && deck.Count < lowDeckCount)
//             deck.RebuildAndShuffle();

//         phase = CombatPhase.DefenseDraw;
//         Debug.Log($"========== YENİ EL ==========\nEşik: {threshold}\nÖnce DEF toplanacak. (H = Hit, J = Stand)");

//         // Başlangıç görünümü için 0 / threshold gönder
//         onDefProgress?.Invoke(0, threshold);
//         onAtkProgress?.Invoke(0, threshold);

//         AnnounceTotals();
//     }

//     private void NextPhase()
//     {
//         switch (phase)
//         {
//             case CombatPhase.DefenseDraw:
//                 phase = CombatPhase.AttackDraw;
//                 Debug.Log($"---------- FAZ GEÇİŞİ ----------\nDEF tamamlandı: {defAcc.Total}\nŞimdi ATK topluyorsun. (H = Hit, J = Stand)");
//                 break;

//             case CombatPhase.AttackDraw:
//                 phase = CombatPhase.Resolve;
//                 Debug.Log("---------- ÇÖZÜMLEME ----------");
//                 Resolve();
//                 Debug.Log("---------- EL BİTTİ ---------- (N = Yeni El)");
//                 break;

//             case CombatPhase.Resolve:
//                 StartNewTurn();
//                 break;
//         }
//     }

//     private void Resolve()
//     {
//         // 1) Oyuncu Block kazanır
//         int defVal = defAcc.Total;
//         if (defVal > 0) player.GainBlock(defVal);
//         else Debug.Log("[Resolve] DEF fazı bust → 0 Block.");

//         // 2) Oyuncu saldırır
//         int atkVal = atkAcc.Total;
//         if (atkVal > 0) enemy.TakeDamage(atkVal);
//         else Debug.Log("[Resolve] ATK fazı bust → 0 Hasar.");

//         // 3) Basit düşman saldırısı (örnek)
//         int enemyAtk = 10;
//         Debug.Log($"[EnemyTurn] Düşman saldırısı: {enemyAtk}");
//         player.TakeDamage(enemyAtk);

//         Debug.Log($"[State] Oyuncu HP: {player.CurrentHP}/{player.MaxHP}  Block: {player.Block} | Düşman HP: {enemy.CurrentHP}/{enemy.MaxHP}");
//     }

//     private void AnnounceTotals()
//     {
//         Debug.Log($"[TOPLAMLAR] DEF: {defAcc.Total} | ATK: {atkAcc.Total}");
//     }

//     // İsteğe bağlı: Eşik değişimi
//     public void SetThreshold(int newVal)
//     {
//         threshold = Mathf.Max(5, newVal);
//         Debug.Log($"[Rule] Eşik {threshold} olarak ayarlandı.");
//         AnnounceTotals();
//     }
// }
