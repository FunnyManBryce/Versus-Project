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

    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;

    public float chasePlayerDistance = 10;
    public float chaseMinionDistance = 10;
    public float attackDistance = 5;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 2f;
    public float cooldownTimer = 0f;

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
        Health.Value = 50;
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                if(Team == 1)
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
        if (!IsServer) return;
        animator.SetBool("Attacking", isAttacking);
        if (isAttacking) return;
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
        } else if (cooldownTimer >= cooldownLength)
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
        }
        else if (distanceFromMinion.magnitude < chaseMinionDistance && aggro == false)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyMinionTarget.position);
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
        }
        else if (distanceFromPlayer.magnitude < chasePlayerDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyPlayer.transform.position);
            distanceFromTarget = distanceFromPlayer;
            currentTarget = enemyPlayer.GetComponent<NetworkObject>();
        }
        if (distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            agent.speed = 0;
            isAttacking = true;
        }

    }

    public void DealDamage()
    {
        if(currentTarget != null)
        {
            DealDamageServerRPC(20, currentTarget, networkMinion); //Minions can't attack tower because pathfinding radius away from them is too high. Need to make attack distance based on edge of target somehow?

        } else
        {
            isAttacking = false;
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender) 
    {
        Health.Value = Health.Value - damage;
        Debug.Log("AHHHHH HELP ME IM HURT AHHHH");
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
        if(reference.TryGet(out NetworkObject target))
        {
            if(target.tag == "Player")
            {

            } else if(target.tag == "Tower")
            {
                Debug.Log("WHY WOnT YOU DIE");
                target.GetComponent<Tower>().TakeDamageServerRPC(damage, sender);
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
        cooldown = true;
    }



}
