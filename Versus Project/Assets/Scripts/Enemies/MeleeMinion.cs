using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class MeleeMinion : NetworkBehaviour
{
    public int Team;


    public Transform target;
    public Transform minionTarget;
    public NavMeshAgent agent;
    public Animator animator;
    public Animator weaponAttack;
    public Vector3 minionPosition;
    public Vector3 targetPosition;
    public Vector3 distanceFromTarget;
    public bool isAttacking = false;
    public bool cooldown = false;
    public float attackDistance = 2;
    public float moveSpeed = 3;

    public GameObject enemyPlayer;
    public GameObject Minion;
    public GameObject enemyPentagon;
    public GameObject enemyInhibitor;
    public GameObject enemyTower2;
    public GameObject enemyTower1;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        if(Team == 1)
        {
            enemyPentagon = GameObject.Find("Player2Pentagon");
            enemyInhibitor = GameObject.Find("Player2Inhibitor");
            enemyTower2 = GameObject.Find("Player2Tower2");
            enemyTower1 = GameObject.Find("Player2Tower1");
        }
        else
        {
            enemyPentagon = GameObject.Find("Player1Pentagon");
            enemyInhibitor = GameObject.Find("Player1Inhibitor");
            enemyTower2 = GameObject.Find("Player1Tower2");
            enemyTower1 = GameObject.Find("Player1Tower1");
        }
    }


    void Update()
    {
        if (enemyPlayer != null)
        {
            minionPosition = new Vector3(minionTarget.position.x, minionTarget.position.y, 0);
            targetPosition = new Vector3(target.position.x, target.position.y, 0);
            distanceFromTarget = new Vector3(minionPosition.x - targetPosition.x, minionPosition.y - targetPosition.y, 0);
            //animator.SetFloat("Speed", agent.speed);
            //animator.SetBool("Attacking", isAttacking);
            //weaponAttack.SetBool("Attacking", isAttacking);
            if (distanceFromTarget.magnitude > attackDistance && isAttacking == false)
            {
                //agent.speed = moveSpeed;
                //agent.SetDestination(target.position);
            }
            if (distanceFromTarget.magnitude < attackDistance && cooldown == false)
            {
                //agent.speed = 0;
                //isAttacking = true;

            }
        }
    }


}
