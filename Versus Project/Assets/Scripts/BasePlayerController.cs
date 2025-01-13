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
    [SerializeField] NetworkObject currentTarget; // current attack target
    [SerializeField] bool isAutoAttacking = false;
    public GameObject healthBarPrefab; 

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

            GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Screen Space Canvas").transform);
            healthBar.GetComponent<PlayerHealthBar>().enabled = true;
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
        playerInput = new Vector2(0, 0);
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

        // Only cancel auto-attacking if player initiates movement with WASD
        if (playerInput.magnitude > 0)
        {
            currentTarget = null;
            isAutoAttacking = false;
        }

        // Handle clicking on targets
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TryAutoAttack(mousePosition);
        }

        // Update attack state
        if (isAttacking)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= 1f / autoAttackSpeed)
            {
                isAttacking = false;
            }
        }

        // Handle auto-attacking
        if (currentTarget != null && !isAttacking)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= 1f / autoAttackSpeed)
            {
                float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
                if (distanceToTarget <= attackRange)
                {
                    PerformAutoAttack(currentTarget);
                }
            }
        }
    }

    private void TryAutoAttack(Vector3 mousePosition)
    {
        mousePosition.z = 0;
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            NetworkObject targetObject = hit.collider.GetComponent<NetworkObject>();
            if (targetObject != null && CanAttackTarget(targetObject))
            {
                // Set target regardless of distance
                currentTarget = targetObject;
                isAutoAttacking = true;
            }
        }
    }
    private void PerformAutoAttack(NetworkObject targetObject)
    {
        if (targetObject == null || isAttacking) return;

        float distanceToTarget = Vector2.Distance(transform.position, targetObject.transform.position);
        if (distanceToTarget <= attackRange)
        {
            SpawnProjectileServerRpc(new NetworkObjectReference(targetObject), new NetworkObjectReference(NetworkObject));
            isAttacking = true;
            lastAttackTime = Time.time;
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

    [ServerRpc]
    private void SpawnProjectileServerRpc(NetworkObjectReference target, NetworkObjectReference sender)
    {
        if (target.TryGet(out NetworkObject targetObj) && sender.TryGet(out NetworkObject senderObj))
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();

            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(autoAttackProjSpeed, attackDamage, targetObj, senderObj);
        }
    }
    //region deal damage and take damage server rpc
    #region
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
    #endregion
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }

        if (currentTarget != null && playerInput.magnitude == 0)
        {
            // Move toward the target if out of range
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget > attackRange)
            {
                Vector2 directionToTarget = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
                movementInput = directionToTarget;
                currentSpeed += acceleration * maxSpeed * Time.fixedDeltaTime;

                // Update sprite direction
                if (PlayerSprite != null)
                {
                    PlayerSprite.flipX = currentTarget.transform.position.x < transform.position.x;
                }
            }
            else
            {
                // Within attack range, stop moving
                movementInput = Vector2.zero;
                currentSpeed -= deacceleration * maxSpeed * Time.fixedDeltaTime;
            }
        }
        else if (playerInput.magnitude > 0)
        {
            // Handle direct player movement input
            movementInput = playerInput;
            currentSpeed += acceleration * maxSpeed * Time.fixedDeltaTime;
        }
        else
        {
            // No input and no target, decelerate
            movementInput = Vector2.zero;
            currentSpeed -= deacceleration * maxSpeed * Time.fixedDeltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
        rb2d.velocity = movementInput * currentSpeed;
    }
}