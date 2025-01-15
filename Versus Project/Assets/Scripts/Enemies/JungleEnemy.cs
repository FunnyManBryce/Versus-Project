using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class JungleEnemy : NetworkBehaviour
{

    public Health health;
    private LameManager lameManager;

    public NavMeshAgent agent;

    public Transform jungleTarget;
    public Transform playerOneTarget;
    public Transform playerTwoTarget;

    public Animator animator;

    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayerOne;
    private Vector3 distanceFromPlayerTwo;

    public bool Moves = false;
    public bool isRanged;
    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;

    public string targetName;
    public float Damage;
    public float chasePlayerDistance = 10;
    public float range;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;

    public GameObject spawner;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;
    public GameObject jungleEnemy;
    public NetworkObject networkEnemy;
    public NetworkObject currentTarget;

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
                }
            }
            if (health.currentHealth.Value <= 0 && IsServer == true)
            {
                spawner.GetComponent<JungleSpawner>().isSpawnedEnemyAlive = false;
                jungleEnemy.GetComponent<NetworkObject>().Despawn();
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(jungleEnemy, jungleTarget);
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking || !IsServer) return;
        playerOneTarget = PlayerOne.transform;
        playerTwoTarget = PlayerTwo.transform;
        if (cooldown == true && cooldownTimer < cooldownLength)
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
        distanceFromPlayerOne = new Vector3(jungleTarget.position.x - playerOneTarget.position.x, jungleTarget.position.y - playerOneTarget.position.y, 0);
        distanceFromPlayerTwo = new Vector3(jungleTarget.position.x - playerTwoTarget.position.x, jungleTarget.position.y - playerTwoTarget.position.y, 0);
        if (distanceFromPlayerOne.magnitude < distanceFromPlayerTwo.magnitude && distanceFromPlayerOne.magnitude < range && aggro == true && cooldown == false)
        {
            agent.speed = 0;
            currentTarget = PlayerOne.GetComponent<NetworkObject>();
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
        else if (distanceFromPlayerTwo.magnitude < range && aggro == true && cooldown == false)
        {
            agent.speed = 0;
            currentTarget = PlayerTwo.GetComponent<NetworkObject>();
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
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
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, 0);
        }
        else
        {
            Debug.Log("player networkobject fake??");
        }
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
    }
}
