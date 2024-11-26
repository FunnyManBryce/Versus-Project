using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class MeleeMinion : NetworkBehaviour
{
    public int Team;
    public NetworkVariable<float> Health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private LameManager lameManager;

    public Transform towerTarget;
    public Transform minionTarget;
    public Transform enemyMinionTarget;

    public NavMeshAgent agent;

    public Animator animator;
    public Animator weaponAttack;

    private Vector3 distanceFromTower;
    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayer;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;

    public float chasePlayerDistance = 20;
    public float chaseMinionDistance = 10;
    public float attackDistance = 2;
    public float moveSpeed = 3;
    public float aggroTimer = 10f;

    public GameObject enemyPlayer;
    public GameObject enemyMinion;
    public GameObject Minion;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        lameManager = FindObjectOfType<LameManager>();
    }

    void Update()
    {
        if (!IsServer) return;
        if(aggro == true && aggroTimer > 0)
        {
            aggroTimer -= Time.deltaTime;
        } else if(aggroTimer < 0)
        {
            aggroTimer = 10;
            aggro = false;
        }
        if (Team == 1) //This is definitely spaghetti code, but it basically uses a list of every tower and every minion to determine which tower or enemy minion it should go after
        {

            towerTarget = lameManager.teamTwoTowers[lameManager.TowersLeft.Value].transform;
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
            towerTarget = lameManager.teamOneTowers[lameManager.TowersLeft.Value].transform;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamOneMinions)
            {
                Vector3 directionToTarget = new Vector3(minionTarget.position.x - potentialTarget.transform.position.x, minionTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                }
            }
        }
        distanceFromTower = new Vector3(minionTarget.position.x - towerTarget.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
        distanceFromPlayer = new Vector3(minionTarget.position.x - enemyPlayer.transform.position.x, minionTarget.position.y - enemyPlayer.transform.position.y, 0);
        //animator.SetFloat("Speed", agent.speed); //We aren't using animators yet
        //animator.SetBool("Attacking", isAttacking);
        //weaponAttack.SetBool("Attacking", isAttacking);

        if (distanceFromPlayer.magnitude > chasePlayerDistance && distanceFromMinion.magnitude > chaseMinionDistance && aggro == false)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(towerTarget.position);
            distanceFromTarget = distanceFromTower;
        }
        else if (distanceFromMinion.magnitude < chaseMinionDistance && aggro == false)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyMinionTarget.position);
            distanceFromTarget = distanceFromMinion;
        }
        else if (distanceFromPlayer.magnitude < chasePlayerDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyPlayer.transform.position);
            distanceFromTarget = distanceFromPlayer;
        }
        if (distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            agent.speed = 0;
            //isAttacking = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage) 
    {
        Health.Value = Health.Value - damage;
        //something to check if they were hit by the player
        //if so, then set aggro to true and aggro timer to 10 seconds
    }


}
