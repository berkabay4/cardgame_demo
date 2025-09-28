public interface IPlayerInitListener
{
    // Player spawn/init bittiğinde çağrılır
    void OnPlayerInitialized(PlayerData data, PlayerStats stats);
}
