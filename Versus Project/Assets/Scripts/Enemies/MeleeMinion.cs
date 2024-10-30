using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeleeMinion : MonoBehaviour
{
    public int teamNumber;
    public GameObject currentTower;

    public Transform target;
    public Transform enemyTarget;
    public NavMeshAgent agent;
    public Animator animator;
    public Animator weaponAttack;
    public Vector3 enemyPosition;
    public Vector3 playerPosition;
    public Vector3 distanceFromPlayer;
    public bool isAttacking = false;
    public bool cooldown = false;
    public float attackDistance = 2;
    public float moveSpeed = 3;
    public GameObject player;
    public GameObject enemy;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
    }


    void Update()
    {
        if (player != null)
        {
            enemyPosition = new Vector3(enemyTarget.position.x, enemyTarget.position.y, 0);
            playerPosition = new Vector3(target.position.x, target.position.y, 0);
            distanceFromPlayer = new Vector3(enemyPosition.x - playerPosition.x, enemyPosition.y - playerPosition.y, 0);
            animator.SetFloat("Speed", agent.speed);
            animator.SetBool("Attacking", isAttacking);
            weaponAttack.SetBool("Attacking", isAttacking);
            if (distanceFromPlayer.magnitude > attackDistance && isAttacking == false)
            {
                //Debug.Log("Chasing");
                agent.speed = moveSpeed;
                agent.SetDestination(target.position);
            }
            if (distanceFromPlayer.magnitude < attackDistance && cooldown == false)
            {
                //Debug.Log("Attacking");
                agent.speed = 0;
                isAttacking = true;

            }
        }
    }


}
