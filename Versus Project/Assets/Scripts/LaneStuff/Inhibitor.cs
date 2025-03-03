using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Inhibitor : Tower
{
    void Start()
    {
        networkTower = tower.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
    }

    protected override void Update()
    {
        if(Team == 1)
        {
            if (lameManager.teamOneTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            if (lameManager.playerTwoChar != null)
            {
                enemyPlayer = lameManager.playerTwoChar;
            }
        }
        else if(Team == 2)
        {
            if (lameManager.teamTwoTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            if (lameManager.playerOneChar != null)
            {
                enemyPlayer = lameManager.playerOneChar;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.lastAttacker.TryGet(out NetworkObject attacker))
            {
                if (attacker.tag == "Player")
                {
                    playerLastHit = true;
                }
                else
                {
                    playerLastHit = false;
                }
            }
            if (health.currentHealth.Value <= 0 && IsServer == true && dead == false)
            {
                dead = true;
                if (playerLastHit)
                {
                    enemyPlayer.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                }
                if (Team == 1)
                {
                    lameManager.teamOneInhibAlive = false;
                } else if(Team == 2)
                {
                    lameManager.teamTwoInhibAlive = false;
                }
                lameManager.TowerDestroyedServerRPC(Team);
                tower.GetComponent<NetworkObject>().Despawn();
            }
        };
        health.invulnerable = true;
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(tower, towerTarget, 2f);
    }
}
