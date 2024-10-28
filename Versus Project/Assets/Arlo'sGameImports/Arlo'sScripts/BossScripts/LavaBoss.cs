using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaBoss : MonoBehaviour
{
    public bool isTeleporting;
    public bool onCooldown;
    public bool attackPhase;
    public bool isAttacking;
    public bool isSpawning;

    public Health health;

    public GameObject Pyromancer;
    public GameObject Player;
    public GameObject Gerald;
    public GameObject projectileTracking;
    public GameObject[] possibleAttacks;

    public Transform bossTarget;
    public Transform playerTarget;

    public Vector3[] teleportLocations;
    Vector3 bossPosition;
    Vector3 playerPosition;
    Vector3 distanceFromPlayer;
    public Animator animator;

    public int maxAttacks;
    public int currentAttacks;
    public int maxTeleports;
    public int currentTeleports;
    public int maxSummons;
    public int currentSummons;
    
    public float attackDelay;
    public float currentAttackDelay;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        playerTarget = Player.transform;
        attackPhase = true;
        isAttacking = true;
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("Teleporting", isTeleporting);
        animator.SetBool("Summoning", isSpawning);
        animator.SetBool("Attacking", isAttacking);
        bossPosition = new Vector3(bossTarget.position.x, bossTarget.position.y, bossTarget.position.z);
        playerPosition = new Vector3(playerTarget.position.x, playerTarget.position.y, playerTarget.position.z);
        distanceFromPlayer = new Vector3(bossPosition.x - playerPosition.x, bossPosition.y - playerPosition.y, bossPosition.z - playerPosition.z);
        if (onCooldown == true)
        {
            currentAttackDelay = currentAttackDelay + Time.deltaTime;
            if(currentAttackDelay > attackDelay)
            {
                currentAttackDelay = 0;
                onCooldown = false;
                isAttacking = true;
            }
        }
        if (health.currentHealth <= 300 && health.currentHealth > 100)
        {
            maxAttacks = 7;
            maxTeleports = 6;
        }
        if (health.currentHealth <= 100)
        {
            maxAttacks = 10;
            maxTeleports = 7;
        }
    }
    
    public void Teleport()
    {
        FindObjectOfType<BryceAudioManager>().Play("Boss Teleport");
        Gerald.transform.position = teleportLocations[Random.Range(0, teleportLocations.Length)];
        currentTeleports++;
        if(currentTeleports == maxTeleports)
        {
            Gerald.transform.position = teleportLocations[Random.Range(0, teleportLocations.Length)];
            isTeleporting = false;
            currentTeleports = 0;
            onCooldown = true;
            attackPhase = true;
        }
    }

    public void Attack()
    {
        FindObjectOfType<BryceAudioManager>().Play("Fire");
        Instantiate(possibleAttacks[Random.Range(0,possibleAttacks.Length)], bossPosition, projectileTracking.transform.rotation);
        currentAttacks++;
        isAttacking = false;
        if (currentAttacks == maxAttacks)
        {
            currentAttacks = 0;
            attackPhase = false;
            isSpawning = true;
        } else
        {
            onCooldown = true;
        }
    }

    public void Summon()
    {
        isSpawning = false;
        if(currentSummons < maxSummons)
        {
            FindObjectOfType<BryceAudioManager>().Play("Boss Summon");
            if (currentSummons == 1)
            {
                Instantiate(Pyromancer, teleportLocations[Random.Range(0, teleportLocations.Length)], Quaternion.identity);
                ArenaManager.enemiesAlive++;
                currentSummons++;
            }
            if(currentSummons == 0)
            {
                Instantiate(Pyromancer, teleportLocations[Random.Range(0, teleportLocations.Length)], Quaternion.identity);
                ArenaManager.enemiesAlive++;
                currentSummons++;
                Instantiate(Pyromancer, teleportLocations[Random.Range(0, teleportLocations.Length)], Quaternion.identity);
                ArenaManager.enemiesAlive++;
                currentSummons++;
            }
            
        }
        isTeleporting = true;
    }

    public void BossDeath()
    {
        ArenaManager.enemiesAlive--;
    }
}
