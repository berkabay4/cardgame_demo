// ResolveAction.cs
using UnityEngine;
public class ResolveAction : IGameAction
{
    public int EnemyFixedAttack = 10; // basit örnek
    public void Execute(CombatContext ctx)
    {
        var pDef = ctx.GetAcc(Actor.Player, PhaseKind.Defense).Total;
        var pAtk = ctx.GetAcc(Actor.Player, PhaseKind.Attack).Total;
        var eDef = ctx.GetAcc(Actor.Enemy, PhaseKind.Defense).Total;
        var eAtk = ctx.GetAcc(Actor.Enemy, PhaseKind.Attack).Total;

        var player = ctx.GetUnit(Actor.Player);
        var enemy = ctx.GetUnit(Actor.Enemy);

        // 1) BLOCK uygula
        if (pDef > 0) player.GainBlock(pDef); else Debug.Log("[Resolve] Player DEF bust → 0 Block");
        if (eDef > 0) enemy.GainBlock(eDef); else Debug.Log("[Resolve] Enemy  DEF bust → 0 Block");

        // 2) Saldırılar
        if (pAtk > 0) enemy.TakeDamage(pAtk); else Debug.Log("[Resolve] Player ATK bust → 0 dmg");
        if (eAtk > 0) player.TakeDamage(eAtk); else Debug.Log("[Resolve] Enemy  ATK bust → 0 dmg");

        Debug.Log($"[State] P HP:{player.CurrentHP}/{player.MaxHP} Block:{player.Block} | E HP:{enemy.CurrentHP}/{enemy.MaxHP} Block:{enemy.Block}");
    }
    public string Describe()=> "Resolve";
}
