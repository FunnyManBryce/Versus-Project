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
    public NavMeshAgent agent;
    public Animator animator;
    public Animator weaponAttack;
    public Vector3 minionPosition;
    public Vector3 targetPosition;
    public Vector3 playerPosition;
    public Vector3 distanceFromTarget;
    public Vector3 distanceFromPlayer;
    public bool isAttacking = false;
    public bool cooldown = false;
    public float chaseDistance = 10;
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
        if(Team ==  1) //This code determins what tower the minion will go after
        {
            target = lameManager.teamTwoTowers[lameManager.TowersLeft.Value].transform;
        } else
        {
            target = lameManager.teamOneTowers[lameManager.TowersLeft.Value].transform;
        }

        minionPosition = new Vector3(minionTarget.position.x, minionTarget.position.y, 0);
        targetPosition = new Vector3(target.position.x, target.position.y, 0);
        playerPosition = new Vector3(enemyPlayer.transform.position.x, enemyPlayer.transform.position.y, 0);
        distanceFromTarget = new Vector3(minionPosition.x - targetPosition.x, minionPosition.y - targetPosition.y, 0);
        distanceFromPlayer = new Vector3(minionPosition.x - playerPosition.x, minionPosition.y - playerPosition.y, 0);
        //animator.SetFloat("Speed", agent.speed);
        //animator.SetBool("Attacking", isAttacking);
        //weaponAttack.SetBool("Attacking", isAttacking);
        if (distanceFromTarget.magnitude > attackDistance && isAttacking == false && distanceFromPlayer.magnitude > chaseDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(target.position);
        } else if(distanceFromPlayer.magnitude < chaseDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(enemyPlayer.transform.position);
        }
        if (distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            agent.speed = 0;
            //isAttacking = true;

        }
    }


}
