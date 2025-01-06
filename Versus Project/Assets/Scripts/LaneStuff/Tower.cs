using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Tower : NetworkBehaviour
{
    public int Team;
    private LameManager lameManager;

    public Transform towerTarget;
    public Transform enemyMinionTarget;

    public Animator animator;

    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;

    public GameObject tower;
    public GameObject enemyPlayer;
    public GameObject enemyMinion;
    public NetworkObject networkTower;
    public NetworkObject currentTarget;

    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayer;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;
    
    public string targetName;

    public float Damage;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 2f;
    public float cooldownTimer = 0f;
    public float towerRange = 10f;
    public float startingHealth;
    public NetworkVariable<float> Health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        networkTower = tower.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
    }

    void Update()
    {
        if (!IsServer || isAttacking) return;
        if (Team == 1 && lameManager.playerTwoChar != null)
        {
            enemyPlayer = lameManager.playerTwoChar;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamTwoMinions)
            {
                Vector3 directionToTarget = new Vector3(towerTarget.position.x - potentialTarget.transform.position.x, towerTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        else if (Team == 2 && lameManager.playerOneChar != null)
        {
            enemyPlayer = lameManager.playerOneChar;
            oldTarget = new Vector3(1000, 1000, 0);
            foreach (GameObject potentialTarget in lameManager.teamOneMinions)
            {
                Vector3 directionToTarget = new Vector3(towerTarget.position.x - potentialTarget.transform.position.x, towerTarget.position.y - potentialTarget.transform.position.y, 0);
                if (oldTarget.magnitude > directionToTarget.magnitude)
                {
                    oldTarget = directionToTarget;
                    distanceFromMinion = directionToTarget;
                    enemyMinionTarget = potentialTarget.transform;
                    enemyMinion = potentialTarget;
                }
            }
        }
        //animator.SetBool("Attacking", isAttacking);
        if (enemyPlayer == null) return;
        distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPlayer.transform.position.x, towerTarget.position.y - enemyPlayer.transform.position.y, 0);
        if (cooldown == true && cooldownTimer <= cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
        }
        else if (cooldownTimer >= cooldownLength)
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
        if (enemyMinion == null)
        {
            distanceFromMinion = new Vector3(100, 100, 0);
        }
        if ((distanceFromMinion.magnitude < distanceFromPlayer.magnitude && aggro == false && enemyMinion != null) || (distanceFromMinion.magnitude < towerRange && aggro == false && enemyMinion != null))
        {
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
            targetName = "Minion";
        }
        else if (distanceFromPlayer.magnitude < distanceFromMinion.magnitude)
        {
            distanceFromTarget = distanceFromPlayer;
            currentTarget = enemyPlayer.GetComponent<NetworkObject>();
            targetName = "Player";
        }
        if (targetName == "Minion" && distanceFromTarget.magnitude < towerRange && cooldown == false && enemyMinion != null)
        {
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
        else if (targetName == "Player" && distanceFromTarget.magnitude < towerRange && cooldown == false)
        {
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
        }
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            Health.Value = startingHealth;
        }
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                lameManager.TowerDestroyedServerRPC(Team);
                tower.GetComponent<NetworkObject>().Despawn();
            }
        };
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            DealDamageServerRPC(Damage, currentTarget, networkTower);
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
    }

    [Rpc(SendTo.Server)]
    public void DealDamageServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            if (target.tag == "Player")
            {
                target.GetComponent<BasePlayerController>().TakeDamageServerRpc(damage, sender);
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
