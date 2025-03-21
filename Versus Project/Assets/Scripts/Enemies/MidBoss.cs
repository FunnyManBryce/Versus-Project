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
    public Vector3[] MinionSpawnPoints;
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
        if(onCooldown && currentCooldown < cooldownDuration)
        {
            currentCooldown += Time.deltaTime;
        } else if (onCooldown && currentCooldown >= cooldownDuration)
        {
            currentAttackType = 0;
            onCooldown = false;
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
            Slam();
            isAttacking = true;
        }
        if (currentAttackType == 2)
        {
            //Should eventually change these to activating animations instead
            ProjectileAttack();
            isAttacking = true;
        }
        if (currentAttackType == 3)
        {
            //Should eventually change these to activating animations instead
            AOESpawn();
            isAttacking = true;
        }
        if (currentAttackType == 4)
        {
            //Should eventually change these to activating animations instead
            Summon();
            isAttacking = true;
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
        currentAttackType = Random.Range(1, 5);
        if(currentAttackType == lastAttackNumber)
        {
            if(lastAttackNumber <= 2)
            {
                currentAttackType += 1;
            } else if(lastAttackNumber >= 3)
            {
                currentAttackType -= 1;
            }
        }
    }
    public void Slam()
    {
        Vector2 pos = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 6);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(slamDamage, networkBoss, 10, false);
            }
        }
        isAttacking = false;
        onCooldown = true;
    }
    public void ProjectileAttack()
    {
        GameObject projectile = Instantiate(Projectile, transform.position, Quaternion.identity);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        netObj.Spawn();
        ProjectileController controller = projectile.GetComponent<ProjectileController>();
        controller.Initialize(18, projDamage, lameManager.playerOneChar.GetComponent<NetworkObject>(), networkBoss, 5);
        isAttacking = false;
        onCooldown = true;
    }

    public void AOESpawn()
    {

        isAttacking = false;
        onCooldown = true;
    }

    public void Summon()
    {

        isAttacking = false;
        onCooldown = true;
    }

}
