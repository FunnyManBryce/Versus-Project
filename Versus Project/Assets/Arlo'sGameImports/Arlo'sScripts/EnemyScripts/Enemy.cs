using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public GameObject Creator;
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

    [SerializeField] int expAmount = 100;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 0;
        //player = GameObject.FindGameObjectWithTag("Player");
        //target = player.transform;
        StartCoroutine(SpawnCooldown());
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

    public void EnemyDeath()
    {
        ExperienceManager.Instance.AddExperience(expAmount);
        ArenaManager.enemiesAlive--;
        FindObjectOfType<BryceAudioManager>().Play("Enemy Death");
    }

    public void SkeletonSummonDeath()
    {
        FindObjectOfType<BryceAudioManager>().Play("Enemy Death");
        ArenaManager.enemiesAlive--;
        if(Creator != null)
        {
            Creator.GetComponent<SkeletonBoss>().currentSummons--;
        }
        Destroy(enemy);
    }

    public void FireBlobSummonDeath()
    {
        FindObjectOfType<BryceAudioManager>().Play("Enemy Death");
        ArenaManager.enemiesAlive--;
          //causes a really minor bug that won't exist if we remove the bug of enemies not getting cleaved properly
        if(Creator != null)
        {
            Creator.GetComponent<PyromancerAI>().currentSummons--;
        }
        Destroy(enemy);
    }
    
    public IEnumerator SpawnCooldown()
    {
        cooldown = true;
        yield return new WaitForSeconds(0.5f);
        cooldown = false;
    }
}
