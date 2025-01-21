using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class Puppet : NetworkBehaviour
{
    public int Team;

    public Health health;
    private LameManager lameManager;

    public Transform towerTarget;
    public Transform puppetPos;
    public Transform minionTarget;
    public Transform jungleTarget;

    public NavMeshAgent agent;

    public Animator animator;

    private Vector3 distanceFromFather;
    private Vector3 distanceFromTower;
    public Vector3 distanceFromTarget;
    public Vector3 distanceFromPlayer;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public bool defensiveMode;
    public bool isAttacking = false;
    public bool cooldown = false;
    public bool dead;

    public string targetName;
    public float Damage;
    public float chaseDistance = 10;
    public float followDistance = 10;
    public float stopFollowDistance = 5;

    public float attackDistance = 3;
    public float moveSpeed = 3;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;

    public GameObject enemyPlayer;
    public GameObject Father;
    public GameObject enemyMinion;
    public GameObject enemyTower;
    public GameObject jungleEnemy;
    public GameObject enemyTarget;
    public GameObject puppet;
    public NetworkObject networkPuppet;
    public NetworkObject currentTarget;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;
    // Start is called before the first frame update
    void Start()
    {
        networkPuppet = puppet.GetComponent<NetworkObject>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        lameManager = FindObjectOfType<LameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking || !IsServer) return;
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
        }
        else if (cooldownTimer >= cooldownLength)
        {
            cooldown = false;
            cooldownTimer = 0;
        }
        distanceFromFather = new Vector3(puppetPos.position.x - Father.transform.position.x, puppetPos.position.y - Father.transform.position.y, 0);
        if(distanceFromFather.magnitude > followDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(Father.transform.position);
        } else if(distanceFromFather.magnitude < stopFollowDistance)
        {
            agent.speed = 0;

        }
        oldTarget = new Vector3(1000, 1000, 0);
        Vector2 pos = new Vector2(Father.transform.position.x, Father.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 5f);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                GameObject potentialTarget = collider.gameObject;
                Vector3 directionToTarget = new Vector3(puppetPos.position.x - potentialTarget.transform.position.x, puppetPos.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromTarget = directionToTarget;
                    enemyTarget = potentialTarget;
                }
            }
        }
        if(distanceFromTarget != null && distanceFromTarget.magnitude > attackDistance) 
        {
            agent.SetDestination(enemyTarget.transform.position);
            agent.speed = moveSpeed;
            currentTarget = enemyTarget.GetComponent<NetworkObject>();
        } else if(distanceFromTarget != null && distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            isAttacking = true;
            //animator.SetBool("Attacking", isAttacking);
            agent.speed = moveSpeed;
            Debug.Log("I am attackin!");
            DealDamage();
        }
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            Debug.Log("attacking");
            DealDamageServerRPC(Damage, currentTarget, puppet);
        }
        else
        {
            isAttacking = false;
            //animator.SetBool("Attacking", isAttacking);
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
        //animator.SetBool("Attacking", isAttacking);
        cooldown = true;
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
                    Father.GetComponent<PuppeteeringPlayerController>().puppetAlive = false;
                    puppet.GetComponent<NetworkObject>().Despawn();
                }
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(puppet, puppetPos);
    }
   
    public bool CanAttackTarget(NetworkObject targetObject)
    {
        // Check if target has a team component
        if (targetObject.TryGetComponent(out BasePlayerController targetPlayer))
        {
            return targetPlayer.teamNumber.Value != Team;
        }

        if (targetObject.TryGetComponent(out Tower targetTower))
        {
            return targetTower.Team != Team;
        }

        if (targetObject.TryGetComponent(out MeleeMinion targetMinion))
        {
            return targetMinion.Team != Team;
        }

        if (targetObject.TryGetComponent(out Inhibitor targetInhibitor))
        {
            return targetInhibitor.Team != Team;
        }

        return true; // Default to allowing attack if no team check is possible
    }
}
