using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Buffers.Text;


public class MidBoss : NetworkBehaviour
{
    public BryceAudioManager bAM;
    public Health health;
    private LameManager lameManager;

    private bool dead;

    public float XPRange;
    public float XPGiven;
    public int goldGiven;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;

    // Start is called before the first frame update
    void Start()
    {
        bAM = FindFirstObjectByType<BryceAudioManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0)
            {
                if (IsServer == true && dead == false)
                {
                    dead = true;
                    if (health.lastAttacker.TryGet(out NetworkObject attacker))
                    {
                        if (attacker.tag == "Player" || attacker.tag == "Puppet")
                        {

                            attacker.GetComponent<BasePlayerController>().XP.Value += XPGiven;
                            attacker.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                            //Special effect for killing midboss
                        }
                    }
                    gameObject.GetComponent<NetworkObject>().Despawn();
                }
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(gameObject, gameObject.transform, 5f);
    }


}
