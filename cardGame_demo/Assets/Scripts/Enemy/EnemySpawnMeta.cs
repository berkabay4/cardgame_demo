using UnityEngine;

public class EnemySpawnMeta : MonoBehaviour
{
    public EnemySpawner source;     // hangi spawner’dan geldi
    public Transform spawnPoint;    // hangi noktadan
    public int spawnIndex = -1;     // spawner.spawnPoints içindeki index

    // İstersen debug için:
    public override string ToString()
        => $"{name} [idx={spawnIndex} point={(spawnPoint? spawnPoint.name : "null")}]";
}
