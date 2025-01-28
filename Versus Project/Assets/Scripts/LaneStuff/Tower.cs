using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Tower : NetworkBehaviour
{
    public int Team;
    protected private LameManager lameManager;

    public Transform towerTarget;
    public Transform enemyMinionTarget;

    public Animator animator;

    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;
    public bool dead;
    public bool playerLastHit = false;

    public GameObject tower;
    public GameObject enemyPlayer;
    public GameObject enemyPuppet;
    public GameObject enemyMinion;
    public NetworkObject networkTower;
    public NetworkObject currentTarget;

    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayer;
    public Vector3 distanceFromPuppet;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public float goldRange;
    public int goldGiven;

    public float Damage;
    public int orderInLane;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 2f;
    public float cooldownTimer = 0f;
    public float towerRange = 10f;
    public Health health;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;

    void Start()
    {
        networkTower = tower.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
    }

    protected virtual void Update()
    {
        if (!IsServer || isAttacking) return;
        if (Team == 1 && lameManager.playerTwoChar != null)
        {
            if (lameManager.teamOneTowersLeft.Value != orderInLane)
            {
                health.invulnerable = true;
            }
            if (lameManager.teamOneTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            enemyPlayer = lameManager.playerTwoChar;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamTwoMinions)
            {
                Vector3 directionToTarget = new Vector3(towerTarget.position.x - potentialTarget.transform.position.x, towerTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        else if (Team == 2 && lameManager.playerOneChar != null)
        {
            if (lameManager.teamTwoTowersLeft.Value != orderInLane)
            {
                health.invulnerable = true;
            }
            if (lameManager.teamTwoTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            enemyPlayer = lameManager.playerOneChar;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamOneMinions)
            {
                Vector3 directionToTarget = new Vector3(towerTarget.position.x - potentialTarget.transform.position.x, towerTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        //animator.SetBool("Attacking", isAttacking);
        if (enemyPlayer == null) return;
        distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPlayer.transform.position.x, towerTarget.position.y - enemyPlayer.transform.position.y, 0);
        if (cooldown == true && cooldownTimer <= cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
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
        if (enemyMinion == null)
        {
            distanceFromMinion = new Vector3(100, 100, 0);
        }
        if ((distanceFromMinion.magnitude < distanceFromPlayer.magnitude && aggro == false && enemyMinion != null) || (distanceFromMinion.magnitude < towerRange && aggro == false && enemyMinion != null))
        {
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
        }
        else if (distanceFromPlayer.magnitude < distanceFromMinion.magnitude)
        {
            if (enemyPlayer.GetComponent<PuppeteeringPlayerController>() != null && enemyPlayer.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
            {
                oldTarget = new Vector3(1000, 1000, 0);
                foreach (GameObject puppet in enemyPlayer.GetComponent<PuppeteeringPlayerController>().PuppetList)
                {
                    Vector3 directionToPuppet = new Vector3(towerTarget.position.x - puppet.transform.position.x, towerTarget.position.y - puppet.transform.position.y, 0);
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
                distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPuppet.transform.position.x, towerTarget.position.y - enemyPuppet.transform.position.y, 0);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPuppet.GetComponent<NetworkObject>();
            }
            else
            {
                distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPlayer.transform.position.x, towerTarget.position.y - enemyPlayer.transform.position.y, 0);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPlayer.GetComponent<NetworkObject>();
            }
        }
        if (distanceFromTarget.magnitude < towerRange && cooldown == false && currentTarget != null)
        {
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
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
                if (distanceFromPlayer.magnitude < goldRange)
                {
                    enemyPlayer.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                }
                lameManager.TowerDestroyedServerRPC(Team);
                tower.GetComponent<NetworkObject>().Despawn();
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(tower, towerTarget);
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            DealDamageServerRPC(Damage, currentTarget, networkTower);
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
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, 0);
        }
        else
        {
            Debug.Log("This is bad");
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
    }
}
