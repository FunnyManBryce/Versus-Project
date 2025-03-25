using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class JungleSpawner : NetworkBehaviour
{
    private float spawnTimer = 0f;
    [SerializeField] private float spawnTimerEnd;
    public bool onlySpawnedOnce = false;

    public bool isSpawnedEnemyAlive = false;
    public GameObject enemyToSpawn;
    public GameObject jungleSpawner;

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        if(!isSpawnedEnemyAlive && spawnTimer < spawnTimerEnd)
        {
            spawnTimer += Time.deltaTime;
        } else if(spawnTimer >= spawnTimerEnd && !isSpawnedEnemyAlive)
        {
            isSpawnedEnemyAlive = true;
            SpawnEnemyServerRPC();
            spawnTimer = 0f;
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnEnemyServerRPC()
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
