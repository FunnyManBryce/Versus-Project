using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class MeleeMinion : NetworkBehaviour
{
    public int Team;

    private LameManager lameManager;

    public Transform target;
    public Transform minionTarget;
    public Transform enemyMinionTarget;

    public NavMeshAgent agent;

    public Animator animator;
    public Animator weaponAttack;

    public Vector3 minionPosition;
    public Vector3 enemyMinionPosition;
    public Vector3 targetPosition;
    public Vector3 playerPosition;
    public Vector3 distanceFromTarget;
    public Vector3 distanceFromPlayer;
    public Vector3 distanceFromMinion;
    public Vector3 oldTarget;

    public bool isAttacking = false;
    public bool cooldown = false;

    public float chasePlayerDistance = 10;
    public float chaseMinionDistance = 5;
    public float attackDistance = 2;
    public float moveSpeed = 3;

    public GameObject enemyPlayer;
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
        if (Team == 1) //This is definitely spaghetti code, but it basically uses a list of every tower and every minion to determine which tower or enemy minion it should go after
        {

            target = lameManager.teamTwoTowers[lameManager.TowersLeft.Value].transform; 
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamTwoMinions) 
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
        else
        {
            target = lameManager.teamOneTowers[lameManager.TowersLeft.Value].transform; 
            oldTarget = new Vector3(1000,1000, 0);
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
        minionPosition = new Vector3(minionTarget.position.x, minionTarget.position.y, 0);
        targetPosition = new Vector3(target.position.x, target.position.y, 0);
        playerPosition = new Vector3(enemyPlayer.transform.position.x, enemyPlayer.transform.position.y, 0);
        distanceFromTarget = new Vector3(minionPosition.x - targetPosition.x, minionPosition.y - targetPosition.y, 0);
        distanceFromPlayer = new Vector3(minionPosition.x - playerPosition.x, minionPosition.y - playerPosition.y, 0);
        //animator.SetFloat("Speed", agent.speed);
        //animator.SetBool("Attacking", isAttacking);
        //weaponAttack.SetBool("Attacking", isAttacking);
        if (distanceFromTarget.magnitude > attackDistance && isAttacking == false && distanceFromPlayer.magnitude > chasePlayerDistance && distanceFromMinion.magnitude > chaseMinionDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(target.position);
        }
        else if (distanceFromPlayer.magnitude < chasePlayerDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyPlayer.transform.position);
        } 
        else if (distanceFromMinion.magnitude < chaseMinionDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyMinionTarget.position);
        }
        if (distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            agent.speed = 0;
            //isAttacking = true;

        }
    }
    
        


}
