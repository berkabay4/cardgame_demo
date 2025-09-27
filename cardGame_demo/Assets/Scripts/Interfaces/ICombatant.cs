// ICombatant.cs
public interface ICombatant
{
    int CurrentHP { get; set; }
    int MaxHP { get; }
    int Block { get; set; }

    void TakeDamage(int amount); // block'u düşüp kalan HP'den yer
    void GainBlock(int amount);
}
