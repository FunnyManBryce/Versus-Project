using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class JungleEnemy : NetworkBehaviour
{
    public Health health;
    private LameManager lameManager;
    private BasePlayerController playerToGetGold;

    public NavMeshAgent agent;

    public Transform jungleTarget;
    public Transform playerOneTarget;
    public Transform playerTwoTarget;

    public Animator animator;

    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayerOne;
    public Vector3 distanceFromPuppetOne;
    private Vector3 distanceFromPlayerTwo;
    public Vector3 distanceFromPuppetTwo;
    private Vector3 oldTarget;

    public bool Moves = false;
    public bool isRanged;
    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;
    public bool playerLastHit = false;
    public bool midBossSpawn = false;
    public bool dead;
    public bool isStunned;

    public string targetName;
    public float Damage;
    public float armorPen = 1;
    public float chasePlayerDistance = 10;
    public float range;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;
    public string attackType;

    public float XPRange;
    public float XPGiven;
    public int goldGiven;

    public GameObject spawner;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;
    public GameObject puppetOne;
    public GameObject puppetTwo;
    public GameObject jungleEnemy;
    public NetworkObject networkEnemy;
    public NetworkObject currentTarget;
    public GameObject projectilePrefab;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;

    // Start is called before the first frame update
    void Start()
    {
        networkEnemy = jungleEnemy.GetComponent<NetworkObject>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        lameManager = FindObjectOfType<LameManager>();
        PlayerOne = lameManager.playerOneChar;
        PlayerTwo = lameManager.playerTwoChar;
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.lastAttacker.TryGet(out NetworkObject attacker))
            {
                if (attacker.tag == "Player")
                {
                    aggro = true;
                    aggroTimer = 0;
                    playerLastHit = true;
                    playerToGetGold = attacker.GetComponent<BasePlayerController>();
                }
                else if (attacker.tag == "Puppet")
                {
                    aggro = true;
                    aggroTimer = 0;
                    playerToGetGold = attacker.GetComponent<Puppet>().Father.GetComponent<BasePlayerController>();
                    playerLastHit = true;
                }
                else
                {
                    playerLastHit = true;
                }
            }
            if (health.currentHealth.Value <= 0 && IsServer == true && dead == false)
            {
                dead = true;
                if (playerLastHit)
                {
                    playerToGetGold.Gold.Value += goldGiven;
                    playerToGetGold.XP.Value += XPGiven;
                }
                if (spawner != null && spawner.GetComponent<JungleSpawner>() != null)
                {
                    spawner.GetComponent<JungleSpawner>().isSpawnedEnemyAlive = false;
                }
                else if (midBossSpawn && spawner != null)
                {
                    spawner.GetComponent<MidBoss>().minionsAlive--;
                }
                jungleEnemy.GetComponent<NetworkObject>().Despawn();
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(jungleEnemy, jungleTarget, 1.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (isStunned)
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
        if (!IsServer || isAttacking || isStunned) return;
        if (PlayerOne != null)
        {
            playerOneTarget = PlayerOne.transform;
        }
        if (PlayerTwo != null)
        {
            playerTwoTarget = PlayerTwo.transform;
        }
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
        }
        else if (cooldownTimer >= cooldownLength)
        {
            cooldown = false;
            cooldownTimer = 0;
        }
        if (aggro == true && aggroTimer < aggroLength && midBossSpawn == false)
        {
            aggroTimer += Time.deltaTime;
        }
        else if (aggroTimer >= aggroLength && midBossSpawn == false)
        {
            aggro = false;
            aggroTimer = 0;
        }
        else if (midBossSpawn == true)
        {
            aggro = true;
        }
        if (aggro && attackType == "Normal")
        {
            if (PlayerOne != null)
            {
                if (PlayerOne.GetComponent<PuppeteeringPlayerController>() != null && PlayerOne.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
                {
                    oldTarget = new Vector3(1000, 1000, 0);
                    foreach (GameObject puppet in PlayerOne.GetComponent<PuppeteeringPlayerController>().PuppetList)
                    {
                        Vector3 directionToPuppet = new Vector3(jungleTarget.position.x - puppet.transform.position.x, jungleTarget.position.y - puppet.transform.position.y, 0);
                        if (oldTarget.magnitude > directionToPuppet.magnitude)
                        {
                            oldTarget = directionToPuppet;
                            distanceFromPuppetOne = directionToPuppet;
                            puppetOne = puppet;
                        }
                    }
                }
                if (distanceFromPlayerOne.magnitude > distanceFromPuppetOne.magnitude && PlayerOne.GetComponent<PuppeteeringPlayerController>() != null && PlayerOne.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
                {
                    distanceFromPlayerOne = new Vector3(jungleTarget.position.x - puppetOne.transform.position.x, jungleTarget.position.y - puppetOne.transform.position.y, 0);
                    playerOneTarget = puppetOne.GetComponent<Transform>();
                }
                else
                {
                    distanceFromPlayerOne = new Vector3(jungleTarget.position.x - PlayerOne.transform.position.x, jungleTarget.position.y - PlayerOne.transform.position.y, 0);
                    playerOneTarget = PlayerOne.GetComponent<Transform>();
                }
            }
            else
            {
                distanceFromPlayerOne = new Vector3(1000, 1000, 1000);
            }
            if (PlayerTwo != null)
            {
                if (PlayerTwo.GetComponent<PuppeteeringPlayerController>() != null && PlayerTwo.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
                {
                    oldTarget = new Vector3(1000, 1000, 0);
                    foreach (GameObject puppet in PlayerTwo.GetComponent<PuppeteeringPlayerController>().PuppetList)
                    {
                        Vector3 directionToPuppet = new Vector3(jungleTarget.position.x - puppet.transform.position.x, jungleTarget.position.y - puppet.transform.position.y, 0);
                        if (oldTarget.magnitude > directionToPuppet.magnitude)
                        {
                            oldTarget = directionToPuppet;
                            distanceFromPuppetTwo = directionToPuppet;
                            puppetTwo = puppet;
                        }
                    }
                }
                if (distanceFromPlayerTwo.magnitude > distanceFromPuppetTwo.magnitude && PlayerTwo.GetComponent<PuppeteeringPlayerController>() != null && PlayerTwo.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
                {
                    distanceFromPlayerTwo = new Vector3(jungleTarget.position.x - puppetTwo.transform.position.x, jungleTarget.position.y - puppetTwo.transform.position.y, 0);
                    playerTwoTarget = puppetTwo.GetComponent<Transform>();
                }
                else
                {
                    distanceFromPlayerTwo = new Vector3(jungleTarget.position.x - PlayerTwo.transform.position.x, jungleTarget.position.y - PlayerTwo.transform.position.y, 0);
                    playerTwoTarget = PlayerTwo.GetComponent<Transform>();
                }
            }
            else
            {
                distanceFromPlayerTwo = new Vector3(1000, 1000, 1000);
            }
            if (distanceFromPlayerOne.magnitude < distanceFromPlayerTwo.magnitude && distanceFromPlayerOne.magnitude < range && aggro == true && cooldown == false)
            {
                currentTarget = playerOneTarget.GetComponent<NetworkObject>();
                agent.speed = 0;
                isAttacking = true;
                animator.SetBool("Attacking", isAttacking);
            }
            else if (distanceFromPlayerTwo.magnitude < range && aggro == true && cooldown == false)
            {
                currentTarget = playerTwoTarget.GetComponent<NetworkObject>();
                agent.speed = 0;
                isAttacking = true;
                animator.SetBool("Attacking", isAttacking);
            }
        } else if(aggro && attackType != null && cooldown == false)
        {
            if (attackType == "Slam")
            {
                isAttacking = true;
                animator.SetBool("Attacking", isAttacking);
                //Slam();
            }
            if(attackType == "Projectile")
            {
                Vector3 oldTarget = new Vector3(1000, 1000, 0);
                bool foundTarget = false;
                Vector2 pos = new Vector2(transform.position.x, transform.position.y);
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, range);
                foreach (var collider in hitColliders)
                {
                    if (collider.GetComponent<BasePlayerController>() != null && collider.isTrigger || collider.GetComponent<Puppet>() != null && collider.isTrigger)
                    {
                        foundTarget = true;
                        GameObject potentialTarget = collider.gameObject;
                        Vector3 directionToTarget = new Vector3(transform.position.x - potentialTarget.transform.position.x, transform.position.y - potentialTarget.transform.position.y, 0);
                        if (oldTarget.magnitude > directionToTarget.magnitude)
                        {
                            oldTarget = directionToTarget;
                            distanceFromTarget = directionToTarget;
                            currentTarget = potentialTarget.GetComponent<NetworkObject>();
                        }
                    }
                }
                if (foundTarget && currentTarget != null)
                {
                    isAttacking = true;
                    animator.SetBool("Attacking", isAttacking);
                }
            }
        }
    }

    public void Projectile()
    {
        if (!IsServer) return;
        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<JungleProj>().damage = Damage;
        proj.GetComponent<JungleProj>().target = currentTarget.transform.position;
        proj.GetComponent<JungleProj>().sender = networkEnemy;
        var projNetwork = proj.GetComponent<NetworkObject>();
        projNetwork.Spawn();
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
    }

    public void Slam()
    {
        if (!IsServer) return;
        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, range);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<BasePlayerController>() != null && collider.isTrigger || collider.GetComponent<Puppet>() != null && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(Damage, new NetworkObjectReference(networkEnemy), armorPen, false);
            }
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
    }

    public void MultiAttack()
    {
        if (currentTarget != null)
        {
            Debug.Log(currentTarget.name);
            MultiattackServerRPC(Damage/2, currentTarget, networkEnemy);
        }
    }
    public void DealDamage()
    {
        if (!IsServer) return;
        if (currentTarget != null)
        {
            Debug.Log(currentTarget.name);
            DealDamageServerRPC(Damage, currentTarget, networkEnemy);
        }
        else
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    [Rpc(SendTo.Server)]
    public void DealDamageServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, armorPen, false);
        }
        else
        {
            Debug.Log("player networkobject fake??");
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
    }

    [Rpc(SendTo.Server)]
    public void MultiattackServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, armorPen, false);
        }
        else
        {
            Debug.Log("player networkobject fake??");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Attack Damage")
        {
            Damage += amount;
            if (Damage <= 1f)
            {
                amount = -Damage + 1f + amount;
                Damage = 1f;
            }
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
            cooldownLength -= amount;
            if (cooldownLength <= 0.1f)
            {
                amount = -cooldownLength + 0.1f + amount;
                cooldownLength = 0.1f;
            }
        }
        if (buffType == "Speed")
        {
            moveSpeed += amount;
            if (moveSpeed <= 0.5f)
            {
                amount = -moveSpeed + 0.5f + amount;
                moveSpeed = 0.5f;
            }
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
        if (buffType == "Immobilized")
        {
            amount = moveSpeed;
            moveSpeed = 0;
        }
        if (buffType == "Darkness")
        {
            moveSpeed += amount;
            health.darknessEffect = true;
        }
        if (buffType == "Stun")
        {
            isStunned = true;
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
        if (buffType == "Attack Damage")
        {
            Damage -= amount;
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
            cooldownLength += amount;
        }
        if (buffType == "Speed")
        {
            moveSpeed -= amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue -= amount;
        }
        if (buffType == "Immobilized")
        {
            moveSpeed += amount;
        }
        if (buffType == "Darkness")
        {
            moveSpeed -= amount;
            health.darknessEffect = false;
        }
        if (buffType == "Stun")
        {
            isStunned = false;
        }
    }
}
