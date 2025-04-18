using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MeleeMinion : NetworkBehaviour
{
    public int Team;

    public BryceAudioManager bAM;
    public Health health;
    private LameManager lameManager;

    public Transform towerTarget;
    public Transform minionTarget;
    public Transform enemyMinionTarget;
    [SerializeField] SpriteRenderer minionSprite;

    public NavMeshAgent agent;

    public Animator animator;

    private Vector3 distanceFromTower;
    public Vector3 distanceFromTarget;
    public Vector3 distanceFromPuppet;
    public Vector3 distanceFromPlayer;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public bool isRanged;
    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;
    public bool playerLastHit = false;
    public bool dead;

    public float baseHP;
    public float Damage;
    public float chasePlayerDistance = 10;
    public float chaseMinionDistance = 10;
    public float chaseTowerDistance = 10;
    public float attackDistance = 3;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;
    public float armorPen = 1;

    public float XPRange;
    public float XPGiven;
    public int goldGiven;

    public GameObject enemyPlayer;
    public GameObject enemyPuppet;
    public GameObject enemyMinion;
    public GameObject enemyTower;
    public GameObject Minion;
    public GameObject projectilePrefab;
    public NetworkObject networkMinion;
    public NetworkObject currentTarget;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;

    void Start()
    {
        networkMinion = Minion.GetComponent<NetworkObject>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        lameManager = FindObjectOfType<LameManager>();
        bAM = FindFirstObjectByType<BryceAudioManager>();
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.lastAttacker.TryGet(out NetworkObject attacker))
            {
                if (attacker.tag == "Player" || attacker.tag == "Puppet")
                {
                    aggro = true;
                    aggroTimer = 0;
                    playerLastHit = true;
                }
                else
                {
                    playerLastHit = false;
                }
            }
            if (health.currentHealth.Value <= 0)
            {
                if (IsServer == true && dead == false)
                {
                    dead = true;
                    if (Team == 1)
                    {
                        lameManager.teamOneMinions.Remove(Minion);
                    }
                    else
                    {
                        lameManager.teamTwoMinions.Remove(Minion);
                    }
                    if (playerLastHit)
                    {
                        enemyPlayer.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                    }
                    if (distanceFromPlayer.magnitude < XPRange)
                    {
                        enemyPlayer.GetComponent<BasePlayerController>().XP.Value += XPGiven;
                    }
                    Minion.GetComponent<NetworkObject>().Despawn();
                }
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(Minion, minionTarget, 1.5f);
        if (IsServer)
        {
            lameManager = FindObjectOfType<LameManager>();
            health.healthSetManual = true;
            health.maxHealth.Value = baseHP + (baseHP * lameManager.matchTimer.Value * 0.004f);
            health.currentHealth.Value = health.maxHealth.Value;
        }
    }

    [ClientRpc]
    public void FlipSpriteClientRpc(bool flip)
    {
        if (flip)
        {
            minionSprite.flipX = true;
        }
        else
        {
            minionSprite.flipX = false;
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (agent.desiredVelocity.x < 0)
        {
            minionSprite.flipX = true;
            FlipSpriteClientRpc(true);
        } else
        {
            minionSprite.flipX = false;
            FlipSpriteClientRpc(false);
        }
        if (isAttacking) return;
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
            if (isRanged)
            {
                agent.speed = 0;
            }
        }
        else if (cooldownTimer >= cooldownLength)
        {
            cooldown = false;
            cooldownTimer = 0;
        }
        if (aggro == true && aggroTimer < aggroLength)
        {
            aggroTimer += Time.deltaTime;
        }
        else if (aggroTimer >= aggroLength)
        {
            aggro = false;
            aggroTimer = 0;
        }
        if (Team == 1)
        {
            enemyTower = lameManager.teamTwoTowers[lameManager.teamTwoTowersLeft.Value];
            towerTarget = lameManager.teamTwoTowers[lameManager.teamTwoTowersLeft.Value].transform;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamTwoMinions)
            {
                Vector3 directionToTarget = new Vector3(minionTarget.position.x - potentialTarget.transform.position.x, minionTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        else
        {
            enemyTower = lameManager.teamOneTowers[lameManager.teamOneTowersLeft.Value];
            towerTarget = lameManager.teamOneTowers[lameManager.teamOneTowersLeft.Value].transform;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamOneMinions)
            {
                Vector3 directionToTarget = new Vector3(minionTarget.position.x - potentialTarget.transform.position.x, minionTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        distanceFromTower = new Vector3(minionTarget.position.x - towerTarget.position.x, minionTarget.position.y - towerTarget.position.y, 0);
        if (enemyPlayer != null)
        {
            distanceFromPlayer = new Vector3(minionTarget.position.x - enemyPlayer.transform.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
        }
        else
        {
            distanceFromPlayer = new Vector3(1000, 1000, 1000);
        }
        if (enemyMinion == null)
        {
            distanceFromMinion = new Vector3(1000, 1000, 0);
        }
        if ((distanceFromTower.magnitude < chaseTowerDistance && aggro == false) || (distanceFromMinion.magnitude > chaseMinionDistance && distanceFromPlayer.magnitude > chasePlayerDistance && aggro == false))
        {
            agent.speed = moveSpeed;
            agent.SetDestination(towerTarget.position);
            distanceFromTarget = distanceFromTower;
            currentTarget = enemyTower.GetComponent<NetworkObject>();
        }
        else if (distanceFromMinion.magnitude < chaseMinionDistance && aggro == false && enemyMinionTarget != null)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyMinionTarget.position);
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
        }
        else if (distanceFromPlayer.magnitude < chasePlayerDistance && aggro == false || aggro == true)
        {
            if (enemyPlayer.GetComponent<PuppeteeringPlayerController>() != null && enemyPlayer.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
            {
                oldTarget = new Vector3(1000, 1000, 0);
                foreach (GameObject puppet in enemyPlayer.GetComponent<PuppeteeringPlayerController>().PuppetList)
                {
                    Vector3 directionToPuppet = new Vector3(minionTarget.position.x - puppet.transform.position.x, minionTarget.position.y - puppet.transform.position.y, 0);
                    if (oldTarget.magnitude > directionToPuppet.magnitude)
                    {
                        oldTarget = directionToPuppet;
                        distanceFromPuppet = directionToPuppet;
                        enemyPuppet = puppet;
                    }
                }
            }
            if (distanceFromPlayer.magnitude > distanceFromPuppet.magnitude && enemyPlayer.GetComponent<PuppeteeringPlayerController>() != null && enemyPlayer.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
            {
                distanceFromPlayer = new Vector3(minionTarget.position.x - enemyPuppet.transform.position.x, minionTarget.position.y - enemyPuppet.transform.position.y, 0);
                agent.SetDestination(enemyPuppet.transform.position);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPuppet.GetComponent<NetworkObject>();

            }
            else
            {
                distanceFromPlayer = new Vector3(minionTarget.position.x - enemyPlayer.transform.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
                agent.SetDestination(enemyPlayer.transform.position);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPlayer.GetComponent<NetworkObject>();
            }
            agent.speed = moveSpeed;
        }
        if (distanceFromTarget.magnitude < attackDistance && cooldown == false && currentTarget != null)
        {
            agent.speed = 0;
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
            if (!isRanged)
            {
                //Melee sound effect
                bAM.PlayServerRpc("Melee Minion Slash", Minion.transform.position);
                bAM.PlayClientRpc("Melee Minion Slash", Minion.transform.position);
            }
        }
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            if (isRanged)
            {
                SpawnProjectileServerRpc(Damage, currentTarget, networkMinion);
                //Ranged sound effect
                bAM.PlayServerRpc("Ranged Minion Fire", Minion.transform.position);
                bAM.PlayClientRpc("Ranged Minion Fire", Minion.transform.position);
            }
            else
            {
                DealDamageServerRPC(Damage, currentTarget, networkMinion);
            }
        }
        else
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileServerRpc(float damage, NetworkObjectReference target, NetworkObjectReference sender)
    {
        if (target.TryGet(out NetworkObject targetObj) && sender.TryGet(out NetworkObject senderObj))
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();

            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(18, damage, targetObj, senderObj, armorPen);
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
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
            Debug.Log("This is bad");
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
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
        if (buffType == "Darkness")
        {
            moveSpeed -= amount;
            health.darknessEffect = false;
        }
    }

}
