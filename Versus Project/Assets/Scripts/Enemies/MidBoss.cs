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
    public bool isAttacking;

    public bool playerInRange;
    public Vector3 distanceFromPOne;
    public Vector3 distanceFromPTwo;

    public bool onCooldown;
    public float cooldownDuration;
    private float currentCooldown;

    public float projDamage;
    public float slamDamage;
    public float AOEDamage;
    public GameObject Projectile;
    public GameObject minionSpawn;
    public GameObject AOEObject;
    public Vector3[] AOESpawnPoints;
    public List<int> allowedSpawnPoints = new List<int>();
    public Vector3[] MinionSpawnPoints;
    public int minionsAlive;
    //public GameObject SlamObject;


    public int currentAttackType;
    public int lastAttackNumber;

    public float XPGiven;
    public int goldGiven;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;
    public NetworkObject networkBoss;

    // Start is called before the first frame update
    void Start()
    {
        bAM = FindFirstObjectByType<BryceAudioManager>();
        lameManager = FindObjectOfType<LameManager>();

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        distanceFromPOne = new Vector3(gameObject.transform.position.x - lameManager.playerOneChar.transform.position.x, gameObject.transform.position.y - lameManager.playerOneChar.transform.position.y, gameObject.transform.position.z - lameManager.playerOneChar.transform.position.z);
        if (lameManager.playerTwoChar != null)
        {
            distanceFromPTwo = new Vector3(gameObject.transform.position.x - lameManager.playerTwoChar.transform.position.x, gameObject.transform.position.y - lameManager.playerTwoChar.transform.position.y, gameObject.transform.position.z - lameManager.playerTwoChar.transform.position.z);
        } else
        {
            distanceFromPTwo = new Vector3(1000,1000, 1000);
        }
        if (distanceFromPOne.magnitude <= 20 || distanceFromPTwo.magnitude <= 20)
        {
            playerInRange = true;
        } else
        {
            playerInRange = false;
        }
        if (!playerInRange) return;
        if(onCooldown && currentCooldown < cooldownDuration)
        {
            currentCooldown += Time.deltaTime;
        } else if (onCooldown && currentCooldown >= cooldownDuration)
        {
            currentAttackType = 0;
            onCooldown = false;
            isAttacking = false;
            currentCooldown = 0;

        }
        if (isAttacking || onCooldown) return;
        if(currentAttackType == 0)
        {
            RandomizeAttackType();
        }
        if (currentAttackType == 1)
        {
            //Should eventually change these to activating animations instead
            isAttacking = true;
            Slam();
        }
        if (currentAttackType == 2)
        {
            //Should eventually change these to activating animations instead
            isAttacking = true;
            ProjectileAttack();
        }
        if (currentAttackType == 3)
        {
            //Should eventually change these to activating animations instead
            isAttacking = true;
            AOESpawn();
        }
        if (currentAttackType == 4)
        {
            //Should eventually change these to activating animations instead
            isAttacking = true;
            Summon();
        }
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

    public void RandomizeAttackType()
    {
        if(minionsAlive == 0)
        {
            currentAttackType = Random.Range(1, 5);
        } else
        {
            currentAttackType = Random.Range(1, 4);
        }
        if (currentAttackType == lastAttackNumber)
        {
            if(lastAttackNumber <= 2)
            {
                currentAttackType += 1;
            } else if(lastAttackNumber >= 3)
            {
                currentAttackType -= 1;
            }
        }
        lastAttackNumber = currentAttackType;
    }
    public void Slam()
    {
        //needs animation windup
        Vector2 pos = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 6);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && collider.tag != "JungleEnemy" && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(slamDamage, networkBoss, 10, false);
            }
        }
        onCooldown = true;
    }
    public void ProjectileAttack()
    {
        if(distanceFromPTwo.magnitude < distanceFromPOne.magnitude)
        {
            GameObject projectile = Instantiate(Projectile, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();
            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(18, projDamage, lameManager.playerTwoChar.GetComponent<NetworkObject>(), networkBoss, 5);
        } else if(distanceFromPTwo.magnitude > distanceFromPOne.magnitude)
        {
            GameObject projectile = Instantiate(Projectile, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();
            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(18, projDamage, lameManager.playerOneChar.GetComponent<NetworkObject>(), networkBoss, 5);
        }
        //Make target closest player probably through circle thingy
        onCooldown = true;
    }

    public void AOESpawn()
    {
        allowedSpawnPoints.Clear();
        for (int i = 0; i < 8; i++)
        {
            allowedSpawnPoints.Add(i);
        }
        for (int i = 0; i < 4; i++)
        {
            int spawnPoint = allowedSpawnPoints[Random.Range(0, allowedSpawnPoints.Count)];
            allowedSpawnPoints.Remove(spawnPoint);
            GameObject AOE = Instantiate(AOEObject, transform.position + AOESpawnPoints[spawnPoint], Quaternion.identity);
            NetworkObject netObj = AOE.GetComponent<NetworkObject>();
            MidbossCircles circleScript = AOE.GetComponent<MidbossCircles>();
            circleScript.damage = AOEDamage;
            circleScript.sender = netObj;
            netObj.Spawn();
        }
        onCooldown = true;
    }

    public void Summon()
    {
        for (int i = 0; i < MinionSpawnPoints.Length; i++)
        {
            GameObject summon = Instantiate(minionSpawn, transform.position + MinionSpawnPoints[i], Quaternion.identity);
            NetworkObject netObj = summon.GetComponent<NetworkObject>();
            netObj.Spawn();
            summon.GetComponent<JungleEnemy>().midBossSpawn = true;
            summon.GetComponent<JungleEnemy>().spawner = gameObject;
            minionsAlive++;
        }
        onCooldown = true;
    }

}
