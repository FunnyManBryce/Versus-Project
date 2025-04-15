using Unity.Netcode;
using UnityEngine;

public class JungleSpawner : NetworkBehaviour
{
    private float spawnTimer = 0f;
    [SerializeField] private float spawnTimerEnd;
    public bool onlySpawnedOnce = false;
    public bool isRandom;
    public GameObject[] SpawnOptions;

    public bool isSpawnedEnemyAlive = false;
    public GameObject enemyToSpawn;
    public GameObject jungleSpawner;

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        if (!isSpawnedEnemyAlive && spawnTimer < spawnTimerEnd)
        {
            spawnTimer += Time.deltaTime;
        }
        else if (spawnTimer >= spawnTimerEnd && !isSpawnedEnemyAlive)
        {
            isSpawnedEnemyAlive = true;
            SpawnEnemyServerRPC();
            spawnTimer = 0f;
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnEnemyServerRPC()
    {
        if (isRandom)
        {
            var enemy = Instantiate(SpawnOptions[Random.Range(0, SpawnOptions.Length)], jungleSpawner.transform.position, Quaternion.identity);
            var enemyNetworkObject = enemy.GetComponent<NetworkObject>();
            enemyNetworkObject.Spawn();
            if (!onlySpawnedOnce)
            {
                enemy.GetComponent<JungleEnemy>().spawner = jungleSpawner;
            }
        }
        else
        {
            var enemy = Instantiate(enemyToSpawn, jungleSpawner.transform.position, Quaternion.identity);
            var enemyNetworkObject = enemy.GetComponent<NetworkObject>();
            enemyNetworkObject.Spawn();
            if (!onlySpawnedOnce)
            {
                enemy.GetComponent<JungleEnemy>().spawner = jungleSpawner;
            }
        }
    }
}
