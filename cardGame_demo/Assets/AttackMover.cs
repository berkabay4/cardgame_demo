using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackMover : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField, Min(0f)] float travelTime = 0.25f;
    [SerializeField, Min(0f)] float returnTime = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    [Tooltip("Hedefe yaklaşırken bırakılacak mesafe (hedefe doğru).")]
    [SerializeField, Min(0f)] float approachDistance = 0.35f;

    [Tooltip("Saldırganın kök (root) transform’u mu taşınsın? (Prefablarda genelde daha doğru olur)")]
    [SerializeField] bool moveRoot = true;

    [Tooltip("Saldırgan hedefe doğru yüzsün mü? (2D’de scale.x flip).")]
    [SerializeField] bool faceTarget = false;

    // aynı saldırgan için üst üste çağrıları yönetmek için
    readonly Dictionary<Transform, Coroutine> running = new();

    void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    IEnumerator SubscribeWhenReady()
    {
        // Coordinator hazır olana kadar bekle (ilk frame kaçırmalarını önler)
        while (GameDirector.Instance == null) yield return null;

        // Çift abonelik olmasın diye önce temizlik
        GameDirector.Instance.onAttackAnimationRequest.RemoveListener(OnAttackRequested);
        GameDirector.Instance.onAttackAnimationRequest.AddListener(OnAttackRequested);

        // Basit bir test logu (isteğe bağlı)
        // Debug.Log("[AttackMover] Subscribed to onAttackAnimationRequest");
    }

    void OnDisable()
    {
        if (GameDirector.Instance)
            GameDirector.Instance.onAttackAnimationRequest.RemoveListener(OnAttackRequested);
        running.Clear();
    }

    void OnAttackRequested(SimpleCombatant attacker, SimpleCombatant defender, int damage)
    {
        if (!attacker || !defender)
        {
            GameDirector.Instance?.AnimReportDone();
            return;
        }

        var a = GetMoveTransform(attacker);   // <<< DEĞİŞTİ
        var d = GetMoveTransform(defender);   // <<< DEĞİŞTİ

        if (running.TryGetValue(a, out var co) && co != null) StopCoroutine(co);

        running[a] = StartCoroutine(DoDash(a, d,
            onImpact: () => GameDirector.Instance?.AnimReportImpact(),
            onDone: () =>
            {
                GameDirector.Instance?.AnimReportDone();
                running.Remove(a);
            }));
    }

    // Hangi transform taşınacak?
    Transform GetMoveTransform(SimpleCombatant sc)
    {
        if (!sc) return null;

        // 1) Eğer prefabta "moveAnchor" adında bir child varsa onu kullan
        var anchor = sc.transform.Find("moveAnchor");
        if (anchor) return anchor;

        // 2) Yoksa doğrudan kendi transform’u
        return sc.transform;

        // NOT: Kesinlikle sc.transform.root DÖNDÜRME!
        // Bu, sahne kökünü hareket ettirip herkesi taşır.
    }
    IEnumerator DoDash(Transform attacker, Transform defender, System.Action onImpact, System.Action onDone)
    {
        var startPos = attacker.position;

        // Hedefe doğru yaklaşım noktası
        Vector3 toDef = defender.position - startPos;
        toDef.z = 0f;
        Vector3 targetPos = defender.position - toDef.normalized * approachDistance;
        targetPos.z = startPos.z; // 2D sahnede z sabit kalsın

        // Yüze çevir (2D sprite için scale.x flip)
        if (faceTarget && Mathf.Abs(toDef.x) > 0.001f)
        {
            var ls = attacker.localScale;
            ls.x = Mathf.Sign(toDef.x) * Mathf.Abs(ls.x);
            attacker.localScale = ls;
        }

        // Gidiş
        yield return LerpMove(attacker, startPos, targetPos, travelTime, ease);

        // Impact (hasar koordinatörde uygulanacak)
        onImpact?.Invoke();
        yield return null; // istersen efekt süresi ver

        // Dönüş
        yield return LerpMove(attacker, attacker.position, startPos, returnTime, ease);

        onDone?.Invoke();
    }

    IEnumerator LerpMove(Transform t, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            t.position = to;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float a = Mathf.Clamp01(timer / duration);
            float k = curve != null ? curve.Evaluate(a) : a;
            t.position = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        t.position = to;
    }
}
