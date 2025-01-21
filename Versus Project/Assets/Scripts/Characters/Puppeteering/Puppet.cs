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
    public float chasePlayerDistance = 10;
    public float chaseMinionDistance = 10;
    public float chaseTowerDistance = 10;
    public float attackDistance = 3;
    public float moveSpeed = 3;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;

    public GameObject enemyPlayer;
    public GameObject Father;
    public GameObject enemyMinion;
    public GameObject enemyTower;
    public GameObject jungleEnemy;
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
        
    }
}
