using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class BasePlayerController : NetworkBehaviour
{
    private Rigidbody2D rb2d;
    public Animator animator;
    [SerializeField] SpriteRenderer PlayerSprite;
    public SpriteRenderer AutoAttackSprite;

    //Movement variables
    public float maxSpeed = 2, acceleration = 50, deacceleration = 100;
    [SerializeField]
    private float currentSpeed = 0;
    private Vector2 movementInput;
    private Vector2 playerInput;

    //Combat variables
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public float maxHealth = 100;
    public float attackDamage = 10f;
    public float autoAttackSpeed = 1f;
    public float autoAttackProjSpeed = 5f;
    public float attackRange = 10f;
    public float lastAttackTime;
    public bool isAttacking = false;
    private float attackCooldown = 0f;

    public bool showAttackRange = true;
    public Color rangeIndicatorColor = new Color(1, 0, 0, 0.2f);

    public GameObject projectilePrefab;
    public NetworkVariable<int> teamNumber = new NetworkVariable<int>();
    

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InitializeHealthServerRpc();

            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);
            Debug.Log("1");
        }
    }

    [ServerRpc]
    private void SetTeamServerRpc(int team)
    {
        teamNumber.Value = team;
    }


    [ServerRpc]
    private void InitializeHealthServerRpc()
    {
        currentHealth.Value = maxHealth;
    }

    private void Start()
    {
        playerInput = new Vector2(0,0);
        lastAttackTime = -autoAttackSpeed;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Get input and store it in playerInput
        Vector2 moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveDir.y = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.y = -1f;
        if (Input.GetKey(KeyCode.A))
        {
            moveDir.x = -1f;
            if (PlayerSprite != null)
            {
                PlayerSprite.flipX = true;
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir.x = +1f;
            if (PlayerSprite != null)
            {
                PlayerSprite.flipX = false;
            }
        }

        playerInput = moveDir.normalized;

        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TryAutoAttack(mousePosition);
            Debug.Log("2");
        }

        if (isAttacking)
        {
            attackCooldown= Time.deltaTime;
            if (attackCooldown <= autoAttackSpeed)
            {
                lastAttackTime = 0;
                isAttacking = false;
            }
        }
    }

    private void TryAutoAttack(Vector3 mousePosition)
    {
        mousePosition.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
        Debug.Log("3");
        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Hit: " + (hit.collider != null ? hit.collider.name : "None"));

        if (hit.collider != null)
        {
            // Check if the hit object is a valid target 
            NetworkObject targetObject = hit.collider.GetComponent<NetworkObject>();
            Debug.Log("4");
            if (targetObject != null)
            {
                Debug.Log("Found Target: " + targetObject.name);
                // Check distance to target
                float distanceToTarget = Vector2.Distance(transform.position, hit.point);
                Debug.Log("5");
                Debug.Log("Distance to Target: " + distanceToTarget + " vs Range: " + attackRange);
                if (distanceToTarget <= attackRange)
                {
                    if (CanAttackTarget(targetObject))
                    {
                        //SpawnProjectileClientRpc(hit.point);

                        DealDamageServerRpc(attackDamage, new NetworkObjectReference(targetObject), new NetworkObjectReference(NetworkObject));

                        isAttacking = true;
                        attackCooldown = 1f / autoAttackSpeed;
                        Debug.Log("6");
                    }
                }
            }
        }
    }
    private bool CanAttackTarget(NetworkObject targetObject)
    {
        // Check if target has a team component
        if (targetObject.TryGetComponent(out BasePlayerController targetPlayer))
        {
            return targetPlayer.teamNumber.Value != teamNumber.Value;
        }

        if (targetObject.TryGetComponent(out Tower targetTower))
        {
            return targetTower.Team != teamNumber.Value;
        }

        if (targetObject.TryGetComponent(out MeleeMinion targetMinion))
        {
            return targetMinion.Team != teamNumber.Value;
        }

        if (targetObject.TryGetComponent(out Inhibitor targetInhibitor))
        {
            return targetInhibitor.Team != teamNumber.Value;
        }

        return true; // Default to allowing attack if no team check is possible
    }

    [ClientRpc]
    private void SpawnProjectileClientRpc(Vector3 targetPosition)
    {
        if (projectilePrefab != null)
            Debug.Log("7");
        {
            
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            Vector2 direction = (targetPosition - transform.position).normalized;
            projectileRb.velocity = direction * autoAttackProjSpeed;
            Debug.Log("8");
        }
    }

[ServerRpc]
private void DealDamageServerRpc(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
{
    if (reference.TryGet(out NetworkObject target))
    {
        Debug.Log("9");
        if (target.tag == "Player")
        {
            // player damage logic
            BasePlayerController playerController = target.GetComponent<BasePlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamageServerRpc(damage, sender);
            }
            Debug.Log("10");
        }
        else if (target.tag == "Tower")
        {
            target.GetComponent<Tower>().TakeDamageServerRPC(damage, sender);
        }
        else if (target.tag == "Minion")
        {
            target.GetComponent<MeleeMinion>().TakeDamageServerRPC(damage, sender);
        }
        else if (target.tag == "Inhibitor")
        {
            target.GetComponent<Inhibitor>().TakeDamageServerRPC(damage, sender);
        }
        else if (target.tag == "JungleEnemy")
        {
            target.GetComponent<JungleEnemy>().TakeDamageServerRPC(damage, sender);
        }
    }
    else
    {
        Debug.Log("BZZZZ wrong answer");
    }
}

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(float damage, NetworkObjectReference sender)
    {
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            //NetworkObject.Despawn();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }

        if (playerInput.magnitude > 0 && currentSpeed >= 0)
        {
            movementInput = playerInput;
            currentSpeed += acceleration * maxSpeed * Time.fixedDeltaTime;
        }
        else
        {
            currentSpeed -= deacceleration * maxSpeed * Time.fixedDeltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
        rb2d.velocity = movementInput * currentSpeed;

    }

    private void OnDrawGizmosSelected()
    {
        if (showAttackRange)
        {
            Gizmos.color = rangeIndicatorColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
