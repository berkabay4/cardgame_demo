using UnityEngine;

/// <summary>Dövüş tipleri: basit minor, elite miniboss veya main boss.</summary>
public enum EnemyFightKind
{
    Minor,
    EliteMiniBoss,
    MainBoss
}

/// <summary>Bir düşman saldırısı tetiklendiğinde davranışlara geçirilen bağlam.</summary>
public struct EnemyAttackContextInfo
{
    /// <summary>Bu combat’taki dövüş tipi.</summary>
    public EnemyFightKind fightKind;

    /// <summary>Kaçıncı el / turn (1,2,3...). Her StartNewTurn’de +1.</summary>
    public int turnIndex;

    /// <summary>Bu elde kaçıncı enemy saldırısı (1,2,3...). Her Resolve sırasında enemy için +1.</summary>
    public int attackRoundIndex;
}
