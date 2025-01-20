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
    public float maxSpeed = 2;
    [SerializeField]
    private float currentSpeed = 0;
    private Vector2 movementInput;
    private Vector2 playerInput;

    //Combat variables
    public float attackDamage = 10f;
    public float autoAttackSpeed = 1f;
    public float autoAttackProjSpeed = 5f;
    public float attackRange = 10f;
    public float lastAttackTime;
    public bool isAttacking = false;
    public float armor = 0f;
    public float cDR = 0f;
    public float armorPen = 0f;
    public float regen = 0f;
    public bool isMelee = false;
    public float maxMana = 0f;
    public float mana = 0f;
    public float manaRegen = 0f;

    private float lastRegenTick = 0f;
    public bool resevoirRegen = false;
    public Health health;
    public float XPToNextLevel;
    public NetworkVariable<float> XP = new NetworkVariable<float>();
    public NetworkVariable<int> Gold = new NetworkVariable<int>();


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
            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);
            Debug.Log("1");

            string canvasName = NetworkManager.LocalClientId == 0 ? "Player1UICanvas" : "Player2UICanvas";
            GameObject playerCanvas = GameObject.Find(canvasName);

            if (playerCanvas != null)
            {
                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;
            }
        }
    }

    [ServerRpc]
    private void SetTeamServerRpc(int team)
    {
        teamNumber.Value = team;
    }

    private protected void Start()
    {
        playerInput = new Vector2(0, 0);
        lastAttackTime = -autoAttackSpeed;
    }

    private protected void Update()
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
        float currentTime = Time.time;
        if (currentTime - lastRegenTick >= 1f) // Check every second
        {
            RegenHealthServerRpc(regen);
            RegenMana(manaRegen);
            lastRegenTick = currentTime;
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
            if (isMelee)
            {
                // melee damage
                DealDamageServerRpc(attackDamage, new NetworkObjectReference(targetObject), new NetworkObjectReference(NetworkObject));
            }
            else
            {
                // Ranged attack with projectile
                SpawnProjectileServerRpc(new NetworkObjectReference(targetObject), new NetworkObjectReference(NetworkObject));
            }
            isAttacking = true;
            lastAttackTime = Time.time;
        }
    }


    public bool CanAttackTarget(NetworkObject targetObject)
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
    //region deal damage server rpc
    #region
    [ServerRpc]
    private void DealDamageServerRpc(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            Debug.Log("9");
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, armorPen);
        }
        else
        {
            Debug.Log("BZZZZ wrong answer");
        }
    }
    #endregion
    private void FixedUpdate()
    {
        if(IsServer)
        {
            if(resevoirRegen == true)
            {
                health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + 1, health.maxHealth.Value);
            }
        }

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
                currentSpeed += maxSpeed;

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
                currentSpeed -= maxSpeed;
            }
        }
        else if (playerInput.magnitude > 0)
        {
            // Handle direct player movement input
            movementInput = playerInput;
            currentSpeed += maxSpeed;
        }
        else
        {
            // No input and no target, decelerate
            movementInput = Vector2.zero;
            currentSpeed -= maxSpeed;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
        rb2d.velocity = movementInput * currentSpeed;
    }
    [ServerRpc]
    private void RegenHealthServerRpc(float regenAmount)
    {
        health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + regenAmount, health.maxHealth.Value);
    }
   
    private void RegenMana(float regenAmount)
    {
        mana = Mathf.Min(mana + regenAmount, maxMana);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Speed")
        {
            maxSpeed += amount;
        }
        if (buffType == "Attack Damage")
        {
            attackDamage += amount;
        }
        if (buffType == "Armor")
        {
            health.armor += amount;
        }
        if (buffType == "Armor Pen")
        {
            armorPen += amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            autoAttackSpeed += amount;
        }
        if (buffType == "Regen")
        {
            regen += amount;
        }
        if (buffType == "Mana Regen")
        {
            manaRegen += amount;
        }
        IEnumerator coroutine = BuffDuration(buffType, amount, duration);
        StartCoroutine(coroutine);
        StatChangeClientRpc(buffType, amount, duration);
    }

    public IEnumerator BuffDuration(string buffType, float amount, float duration) //Waits a bit before changing stats back to default
    {
        yield return new WaitForSeconds(duration);
        BuffEndServerRpc(buffType, amount, duration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuffEndServerRpc(string buffType, float amount, float duration) //changes stat to what it was before
    {
        if (buffType == "Speed")
        {
            maxSpeed -= amount;
        }
        if (buffType == "Attack Damage")
        {
            attackDamage -= amount;
        }
        if (buffType == "Armor")
        {
            health.armor -= amount;
        }
        if (buffType == "Armor Pen")
        {
            armorPen -= amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            autoAttackSpeed -= amount;
        }
        if (buffType == "Regen")
        {
            regen -= amount;
        }
        if (buffType == "Mana Regen")
        {
            manaRegen -= amount;
        }
        StatChangeClientRpc(buffType, -amount, duration);
    }

    [Rpc(SendTo.NotServer)]
    public void StatChangeClientRpc(string buffType, float amount, float duration) //This makes sure that stats are synced to client and host
    {
        if (buffType == "Speed")
        {
            maxSpeed += amount;
        }
        if (buffType == "Attack Damage")
        {
            attackDamage += amount;
        }
        if (buffType == "Armor")
        {
            health.armor += amount;
        }
        if (buffType == "Armor Pen")
        {
            armorPen += amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            autoAttackSpeed += amount;
        }
        if (buffType == "Regen")
        {
            regen += amount;
        }
        if (buffType == "Mana Regen")
        {
            manaRegen += amount;
        }
    }
}