// ActionQueue.cs
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ActionQueue
{
    private readonly Queue<IGameAction> _q = new();
    public void Enqueue(IGameAction a) { _q.Enqueue(a); Debug.Log($"[Queue] + {a.Describe()}"); }
    public void EnqueueRange(IEnumerable<IGameAction> many) { foreach (var a in many) Enqueue(a); }
    public void RunAll(CombatContext ctx) { while (_q.Count > 0) { var a = _q.Dequeue(); Debug.Log($"[Run] {a.Describe()}"); a.Execute(ctx); } }
    public bool HasActions => _q.Count > 0;
    

    public IEnumerator RunAllCoroutine(CombatContext ctx)
    {
        while (_q.Count > 0)
        {
            var a = _q.Dequeue();
            a.Execute(ctx);
            // UI'nin bir frame güncelleyebilmesi için bir frame bekle
            yield return null;
        }
    }
}
