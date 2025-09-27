// IGameAction.cs
public interface IGameAction
{
    // Dönüş senkron; istersen ileride async/Coroutine destekli genişletebilirsin
    void Execute(CombatContext ctx);
    string Describe();
}
