using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class MidBoss : NetworkBehaviour
{
    public BryceAudioManager bAM;
    public Health health;
    private LameManager lameManager;
    public Animator animator;

    public SpriteRenderer bossSprite;
    private bool dead;
    public bool isAttacking;

    public bool playerInRange;
    public Vector3 distanceFromPOne;
    public Vector3 distanceFromPTwo;

    public bool onCooldown;
    public float cooldownDuration;
    private float currentCooldown;


    public float armorPen = 5;
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
        }
        else
        {
            distanceFromPTwo = new Vector3(1000, 1000, 1000);
        }
        if (distanceFromPOne.magnitude <= 15 || distanceFromPTwo.magnitude <= 15)
        {
            playerInRange = true;
            if (distanceFromPTwo.magnitude < distanceFromPOne.magnitude)
            {
                float PTwo = gameObject.transform.position.x - lameManager.playerTwoChar.transform.position.x;
                if(PTwo < 0f)
                {
                    bossSprite.flipX = false;
                    FlipSpriteClientRpc(false);
                } else if(PTwo > 0f)
                {
                    bossSprite.flipX = true;
                    FlipSpriteClientRpc(true);
                }
            }
            else if (distanceFromPTwo.magnitude > distanceFromPOne.magnitude)
            {
                float POne = gameObject.transform.position.x - lameManager.playerOneChar.transform.position.x;
                if (POne < 0f)
                {
                    bossSprite.flipX = false;
                    FlipSpriteClientRpc(false);
                }
                else if (POne > 0f)
                {
                    bossSprite.flipX = true;
                    FlipSpriteClientRpc(true);
                }
            }
        }
        else
        {
            playerInRange = false;
        }
        if (!playerInRange) return;
        if (onCooldown && currentCooldown < cooldownDuration)
        {
            currentCooldown += Time.deltaTime;
        }
        else if (onCooldown && currentCooldown >= cooldownDuration)
        {
            currentAttackType = 0;
            onCooldown = false;
            isAttacking = false;
            currentCooldown = 0;

        }
        if (isAttacking || onCooldown) return;
        if (currentAttackType == 0)
        {
            RandomizeAttackType();
        }
        if (currentAttackType == 1)
        {
            isAttacking = true;
            animator.SetBool("Slam", true);
        }
        if (currentAttackType == 2)
        {
            isAttacking = true;
            animator.SetBool("ProjSummon", true);
        }
        if (currentAttackType == 3)
        {
            isAttacking = true;
            animator.SetBool("AOE", true);
        }
        if (currentAttackType == 4)
        {
            isAttacking = true;
            animator.SetBool("Summon", true);
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
                        if (attacker.tag == "Player")
                        {
                            attacker.GetComponent<BasePlayerController>().XP.Value += XPGiven;
                            attacker.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                            attacker.GetComponent<BasePlayerController>().appliesDarkness.Value = true;
                        }
                        else if (attacker.tag == "Puppet")
                        {
                            attacker = attacker.GetComponent<Puppet>().Father.GetComponent<NetworkObject>();
                            attacker.GetComponent<BasePlayerController>().XP.Value += XPGiven;
                            attacker.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                            attacker.GetComponent<BasePlayerController>().appliesDarkness.Value = true;
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
        if (minionsAlive == 0)
        {
            currentAttackType = Random.Range(1, 5);
        }
        else
        {
            currentAttackType = Random.Range(1, 4);
        }
        if (currentAttackType == lastAttackNumber)
        {
            if (lastAttackNumber <= 2)
            {
                currentAttackType += 1;
            }
            else if (lastAttackNumber >= 3)
            {
                currentAttackType -= 1;
            }
        }
        lastAttackNumber = currentAttackType;
    }

    public void AnimationEnd()
    {
        animator.SetBool("AOE", false);
        animator.SetBool("Summon", false);
        animator.SetBool("Slam", false);
        animator.SetBool("ProjSummon", false);
    }
    public void Slam()
    {
        Vector2 pos = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 7);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && collider.tag != "JungleEnemy" && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(slamDamage, networkBoss, armorPen + 5, false);
            }
        }
        onCooldown = true;
    }
    public void ProjectileAttack()
    {
        if (distanceFromPTwo.magnitude < distanceFromPOne.magnitude)
        {
            GameObject projectile = Instantiate(Projectile, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();
            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(18, projDamage, lameManager.playerTwoChar.GetComponent<NetworkObject>(), networkBoss, armorPen);
        }
        else if (distanceFromPTwo.magnitude > distanceFromPOne.magnitude)
        {
            GameObject projectile = Instantiate(Projectile, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();
            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(18, projDamage, lameManager.playerOneChar.GetComponent<NetworkObject>(), networkBoss, armorPen);
        }
        onCooldown = true;
    }

    public void AOESpawn()
    {
        allowedSpawnPoints.Clear();
        for (int i = 0; i < 8; i++)
        {
            allowedSpawnPoints.Add(i);
        }
        for (int i = 0; i < 3; i++)
        {
            int spawnPoint = allowedSpawnPoints[Random.Range(0, allowedSpawnPoints.Count)];
            allowedSpawnPoints.Remove(spawnPoint);
            GameObject AOE = Instantiate(AOEObject, transform.position + AOESpawnPoints[spawnPoint], Quaternion.identity);
            NetworkObject netObj = AOE.GetComponent<NetworkObject>();
            MidbossCircles circleScript = AOE.GetComponent<MidbossCircles>();
            circleScript.damage = AOEDamage;
            circleScript.sender = gameObject.GetComponent<NetworkObject>();
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

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Attack Damage")
        {
            AOEDamage += amount;
            projDamage += amount;
            slamDamage += amount;
        }
        if (buffType == "Armor")
        {
            health.armor += amount;
            if (health.armor <= 1f)
            {
                amount = -health.armor + 1f + amount;
                health.armor = 1f;
            }
        }
        if (buffType == "Armor Pen")
        {
            armorPen += amount;
            if (armorPen <= 1f)
            {
                amount = -armorPen + 1f + amount;
                armorPen = 1f;
            }
        }
        if (buffType == "Auto Attack Speed")
        {
            cooldownDuration -= amount;
            if (cooldownDuration <= 0.1f)
            {
                amount = -cooldownDuration + 0.1f + amount;
                cooldownDuration = 0.1f;
            }
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
        IEnumerator coroutine = BuffDuration(buffType, amount, duration);
        StartCoroutine(coroutine);
    }

    public IEnumerator BuffDuration(string buffType, float amount, float duration) //Waits a bit before changing stats back to default
    {
        yield return new WaitForSeconds(duration);
        BuffEndServerRpc(buffType, amount, duration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuffEndServerRpc(string buffType, float amount, float duration) //changes stat to what it was before
    {
        if (buffType == "Marked")
        {
            health.markedValue -= amount;
        }
        if (buffType == "Attack Damage")
        {
            AOEDamage -= amount;
            projDamage -= amount;
            slamDamage -= amount;
        }
        if (buffType == "Armor")
        {
            health.armor -= amount;
        }
        if (buffType == "Armor Pen")
        {
            armorPen -= amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            cooldownDuration += amount;
        }
    }

    [ClientRpc]
    public void FlipSpriteClientRpc(bool flip)
    {
        if (flip)
        {
            bossSprite.flipX = true;
        }
        else
        {
            bossSprite.flipX = false;
        }
    }
}
