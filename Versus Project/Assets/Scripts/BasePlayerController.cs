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
    public LameManager lameManager;
    [SerializeField] SpriteRenderer PlayerSprite;
    public SpriteRenderer AutoAttackSprite;
    public ulong clientID;

    //Movement variables
    public float maxSpeed = 2;
    [SerializeField]
    private float currentSpeed = 0;
    private Vector2 movementInput;
    private Vector2 playerInput;

    //Combat variables
    public float[] statGrowthRate;
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
    public float[] XPPerLevel;
    public NetworkVariable<int> unspentUpgrades = new NetworkVariable<int>();
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>();
    public NetworkVariable<int> Level = new NetworkVariable<int>();
    public NetworkVariable<float> XP = new NetworkVariable<float>();
    public NetworkVariable<int> Gold = new NetworkVariable<int>();


    public GameObject projectilePrefab;
    public NetworkVariable<int> teamNumber = new NetworkVariable<int>();
    public NetworkObject currentTarget; // current attack target
    [SerializeField] bool isAutoAttacking = false;
    public GameObject healthBarPrefab;

    private protected void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        lameManager = FindFirstObjectByType<LameManager>();
    }
    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0 && isDead.Value == false && IsServer)
            {
                isDead.Value = true;
            }
        };
        XP.OnValueChanged += (float previousValue, float newValue) => //Checking for Level up
        {
            if (XP.Value >= XPToNextLevel && IsServer)
            {
                LevelUpServerRPC();
            }
        };
        isDead.OnValueChanged += (bool previousValue, bool newValue) => //Checking if dead
        {
            if (isDead.Value)
            {
                transform.position = new Vector3(-420, -69, 0);
                StartCoroutine(lameManager.PlayerDeath(gameObject.GetComponent<NetworkObject>(), lameManager.respawnLength.Value));
            }
            else
            {
                transform.position = lameManager.playerSP[health.Team.Value - 1];
                health.currentHealth.Value = health.maxHealth.Value;
                mana = maxMana;
            }
        };
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
    public void SetTeamServerRpc(int team)
    {
        teamNumber.Value = team;
        health.Team.Value = team;
    }

    private protected void Start()
    {
        playerInput = new Vector2(0, 0);
        lastAttackTime = -autoAttackSpeed;
        if(IsServer)
        {
            LevelUpServerRPC();
        }
    }

    private protected void Update()
    {
        if (!IsOwner) return;
        if(isDead.Value) return;
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
        // Check if target has health component
        if (targetObject.TryGetComponent(out Health targetHealth))
        {
            if (targetHealth.Team.Value == 0)
            {
                return true;
            }
            else
            {
                return targetHealth.Team.Value != health.Team.Value;
            }
        }
        else
        {
            return false;
        }
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
        if(isDead.Value) return;

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
    public void TriggerBuffServerRpc(string buffType, float amount, float duration, bool hasDuration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Speed")
        {
            maxSpeed += amount;
            if (maxSpeed <= 1f)
            {
                amount = -maxSpeed + 1f + amount;
                maxSpeed = 1f;
            }
        }
        if (buffType == "Attack Damage")
        {
            attackDamage += amount;
            if (attackDamage <= 1f)
            {
                amount = -attackDamage + 1f + amount;
                attackDamage = 1f;
            }
        }
        if (buffType == "Armor")
        {
            health.armor += amount;
            if (health.armor <= 1f)
            {
                amount = -health.armor + 1f + amount;
                health.armor = 1f;
            }
        }
        if (buffType == "Armor Pen")
        {
            armorPen += amount;
            if (armorPen <= 1f)
            {
                amount = -armorPen + 1f + amount;
                armorPen = 1f;
            }
        }
        if (buffType == "Auto Attack Speed")
        {
            autoAttackSpeed += amount;
            if (autoAttackSpeed <= 0.1f)
            {
                amount = -autoAttackSpeed + 0.1f + amount;
                autoAttackSpeed = 0.1f;
            }
        }
        if (buffType == "Regen")
        {
            regen += amount;
            if (regen <= 0.1f)
            {
                amount = -regen + 0.1f + amount;
                regen = 0.1f;
            }
        }
        if (buffType == "Mana Regen")
        {
            manaRegen += amount;
            if (manaRegen <= 0.1f)
            {
                amount = -manaRegen + 0.1f + amount;
                manaRegen = 0.1f;
            }
        }
        if (buffType == "Max Mana")
        {
            maxMana += amount;
        }
        if (buffType == "CDR")
        {
            cDR += amount;
        }
        if (buffType == "Health")
        {
            health.maxHealth.Value += amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
        if (buffType == "Immobilized")
        {
            amount = maxSpeed;
            maxSpeed = 0;
        }
        StatChangeClientRpc(buffType, amount);
        if (hasDuration)
        {
            IEnumerator coroutine = BuffDuration(buffType, amount, duration);
            StartCoroutine(coroutine);
        }
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
        if (buffType == "Max Mana")
        {
            maxMana -= amount;
        }
        if (buffType == "CDR")
        {
            cDR -= amount;
        }
        if (buffType == "Health")
        {
            health.maxHealth.Value -= amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue -= amount;
        }
        if (buffType == "Immobilized")
        {
            maxSpeed += amount;
            buffType = "Speed";
        }
        StatChangeClientRpc(buffType, -amount);
    }

    [Rpc(SendTo.NotServer)]
    public void StatChangeClientRpc(string buffType, float amount) //This makes sure that stats are synced to client and host
    {
        if (buffType == "Speed")
        {
            maxSpeed += amount;
            if (maxSpeed <= 1f)
            {
                maxSpeed = 1f;
            }
        }
        if (buffType == "Attack Damage")
        {
            attackDamage += amount;
            if (attackDamage <= 1f)
            {
                attackDamage = 1f;
            }
        }
        if (buffType == "Armor")
        {
            health.armor += amount;
            if (health.armor <= 1f)
            {
                health.armor = 1f;
            }
        }
        if (buffType == "Armor Pen")
        {
            armorPen += amount;
            if (armorPen <= 1f)
            {
                armorPen = 1f;
            }
        }
        if (buffType == "Auto Attack Speed")
        {
            autoAttackSpeed += amount;
            if (autoAttackSpeed <= 0.1f)
            {
                autoAttackSpeed = 0.1f;
            }
        }
        if (buffType == "Regen")
        {
            regen += amount;
            if (regen <= 0.1f)
            {
                regen = 0.1f;
            }
        }
        if (buffType == "Mana Regen")
        {
            manaRegen += amount;
            if (manaRegen <= 0.1f)
            {
                amount = -manaRegen + 0.1f + amount;
                manaRegen = 0.1f;
            }
        }
        if (buffType == "Max Mana")
        {
            maxMana += amount;
        }
        if (buffType == "CDR")
        {
            cDR += amount;
        }
        if (buffType == "Health")
        {
            health.maxHealth.Value += amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
        if (buffType == "Immobilized")
        {
            maxSpeed = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LevelUpServerRPC()
    {
        Level.Value++;
        if (Level.Value > 1)
        {
            TriggerBuffServerRpc("Speed", statGrowthRate[0], 0, false);
            TriggerBuffServerRpc("Attack Damage", statGrowthRate[1], 0, false);
            TriggerBuffServerRpc("Armor", statGrowthRate[2], 0, false);
            TriggerBuffServerRpc("Armor Pen", statGrowthRate[3], 0, false);
            TriggerBuffServerRpc("Auto Attack Speed", statGrowthRate[4], 0, false);
            TriggerBuffServerRpc("Regen", statGrowthRate[5], 0, false);
            TriggerBuffServerRpc("Mana Regen", statGrowthRate[6], 0, false);
            TriggerBuffServerRpc("Max Mana", statGrowthRate[7], 0, false);
            TriggerBuffServerRpc("CDR", statGrowthRate[8], 0, false);
            TriggerBuffServerRpc("Health", statGrowthRate[9], 0, false);
            XP.Value = XP.Value - XPToNextLevel;
        }
        else
        {
            XP.Value = 0;
        }
        XPToNextLevel = XPPerLevel[Level.Value];
        unspentUpgrades.Value++;
    }
}