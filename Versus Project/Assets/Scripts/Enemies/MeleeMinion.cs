using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class MeleeMinion : NetworkBehaviour
{
    public int Team;
    public NetworkVariable<float> Health = new NetworkVariable<float>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private LameManager lameManager;

    public Transform towerTarget;
    public Transform minionTarget;
    public Transform enemyMinionTarget;

    public NavMeshAgent agent;

    public Animator animator;

    private Vector3 distanceFromTower;
    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayer;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public bool isRanged;
    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;

    public string targetName;
    public float Damage;
    public float chasePlayerDistance = 10;
    public float chaseMinionDistance = 10;
    public float minionAttackDistance = 3;
    public float towerAttackDistance = 5;
    public float playerAttackDistance = 4;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;
    public float startingHealth;

    public GameObject enemyPlayer;
    public GameObject enemyMinion;
    public GameObject enemyTower;
    public GameObject Minion;
    public NetworkObject networkMinion;
    public NetworkObject currentTarget;


    void Start()
    {
        networkMinion = Minion.GetComponent<NetworkObject>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        lameManager = FindObjectOfType<LameManager>();
    }

    public override void OnNetworkSpawn()
    {
        Health.Value = startingHealth;
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                if (Team == 1)
                {
                    lameManager.teamOneMinions.Remove(Minion);
                }
                else
                {
                    lameManager.teamTwoMinions.Remove(Minion);
                }
                Minion.GetComponent<NetworkObject>().Despawn();
            }
        };
    }

    void Update()
    {
        if (isAttacking || !IsServer) return;
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
            if(isRanged && distanceFromTarget.magnitude < minionAttackDistance)
            {
                moveSpeed = 0;
                agent.speed = moveSpeed;
            }
        }
        else if (cooldownTimer >= cooldownLength)
        {
            cooldown = false;
            cooldownTimer = 0;
            if(isRanged)
            {
                moveSpeed = 3;
                agent.speed = moveSpeed;
            }
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
        if (Team == 1) //This is definitely spaghetti code, but it basically uses a list of every tower and every minion to determine which tower or enemy minion it should go after
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
        distanceFromTower = new Vector3(minionTarget.position.x - towerTarget.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
        distanceFromPlayer = new Vector3(minionTarget.position.x - enemyPlayer.transform.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
        if (distanceFromPlayer.magnitude > chasePlayerDistance && distanceFromMinion.magnitude > chaseMinionDistance && aggro == false)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(towerTarget.position);
            distanceFromTarget = distanceFromTower;
            currentTarget = enemyTower.GetComponent<NetworkObject>();
            targetName = "Tower";
        }
        else if (distanceFromMinion.magnitude < chaseMinionDistance && aggro == false)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyMinionTarget.position);
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
            targetName = "Minion";
        }
        else if (distanceFromPlayer.magnitude < chasePlayerDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyPlayer.transform.position);
            distanceFromTarget = distanceFromPlayer;
            currentTarget = enemyPlayer.GetComponent<NetworkObject>();
            targetName = "Player";
        }
        if (targetName == "Minion" && distanceFromTarget.magnitude < minionAttackDistance && cooldown == false)
        {
            agent.speed = 0;
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
        else if (targetName == "Player" && distanceFromTarget.magnitude < playerAttackDistance && cooldown == false)
        {
            agent.speed = 0;
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
        else if (targetName == "Tower" && distanceFromTarget.magnitude < towerAttackDistance && cooldown == false)
        {
            agent.speed = 0;
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            DealDamageServerRPC(Damage, currentTarget, networkMinion);
        }
        else
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender)
    {
        Health.Value = Health.Value - damage;
        if (sender.TryGet(out NetworkObject attacker))
        {
            if (attacker.tag == "Player")
            {
                aggro = true;
                aggroTimer = 0;
            }
        }

    }

    [Rpc(SendTo.Server)]
    public void DealDamageServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            if (target.tag == "Player")
            {
                //need something here for when player is real
            }
            else if (target.tag == "Tower")
            {
                target.GetComponent<Tower>().TakeDamageServerRPC(damage, sender);
            }
            else if (target.tag == "Inhibitor")
            {
                target.GetComponent<Inhibitor>().TakeDamageServerRPC(damage, sender);
            }
            else if (target.tag == "Minion")
            {
                target.GetComponent<MeleeMinion>().TakeDamageServerRPC(damage, sender);
            }
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
