using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Relay;
using UnityEngine;

public class BasePlayerController : NetworkBehaviour
{
    public string charName;
    public BryceAudioManager bAM;
    private Rigidbody2D rb2d;
    public Animator animator;
    public LameManager lameManager;
    public SpriteRenderer PlayerSprite;
    public SpriteRenderer AutoAttackSprite;
    public ulong clientID;

    //SuddenDeath
    public NetworkVariable<bool> SuddenDeath = new NetworkVariable<bool>();

    //Movement variables
    public float maxSpeed = 2;
    [SerializeField]
    public float currentSpeed = 0;
    private Vector2 movementInput;
    private Vector2 playerInput;

    //Base Stats
    public float[] statGrowthRate;
    public NetworkVariable<float> BaseDamage = new NetworkVariable<float>();
    public NetworkVariable<float> BaseAttackSpeed = new NetworkVariable<float>();
    public NetworkVariable<float> BaseRange = new NetworkVariable<float>();
    public NetworkVariable<float> BaseCDR = new NetworkVariable<float>();
    public NetworkVariable<float> BaseArmor = new NetworkVariable<float>();
    public NetworkVariable<float> BaseArmorPen = new NetworkVariable<float>();
    public NetworkVariable<float> BaseRegen = new NetworkVariable<float>();
    public NetworkVariable<float> BaseManaRegen = new NetworkVariable<float>();
    public NetworkVariable<float> BaseSpeed = new NetworkVariable<float>();

    //Current buff quantity of stats
    public NetworkVariable<float> DamageBuff = new NetworkVariable<float>();
    public NetworkVariable<float> AttackSpeedBuff = new NetworkVariable<float>();
    public NetworkVariable<float> RangeBuff = new NetworkVariable<float>();
    public NetworkVariable<float> CDRBuff = new NetworkVariable<float>();
    public NetworkVariable<float> ArmorBuff = new NetworkVariable<float>();
    public NetworkVariable<float> ArmorPenBuff = new NetworkVariable<float>();
    public NetworkVariable<float> RegenBuff = new NetworkVariable<float>();
    public NetworkVariable<float> ManaRegenBuff = new NetworkVariable<float>();
    public NetworkVariable<float> SpeedBuff = new NetworkVariable<float>();
    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>();
    public List<IEnumerator> Buffs = new List<IEnumerator>();

    //Combat variables
    public float attackDamage = 10f;
    public float autoAttackSpeed = 1f;
    public float autoAttackProjSpeed = 5f;
    public float attackRange = 10f;
    public float lastAttackTime;
    public bool isAttacking = false;
    public NetworkVariable<bool> appliesDarkness = new NetworkVariable<bool>();
    public float darknessDuration = 120;
    public float cDR = 0f;
    public float armorPen = 0f;
    public float regen = 0f;
    public bool isMelee = false;
    public float maxMana = 0f;
    public float mana = 0f;
    public float manaRegen = 0f;
    public NetworkVariable<float> LifeStealPercentage = new NetworkVariable<float>();
    public NetworkVariable<bool> DebtsDue = new NetworkVariable<bool>();

    private float lastRegenTick = 0f;
    public NetworkVariable<bool> resevoirRegen = new NetworkVariable<bool>();
    public Health health;
    public NetworkVariable<float> XPToNextLevel = new NetworkVariable<float>();
    public float[] XPPerLevel;
    public NetworkVariable<int> unspentUpgrades = new NetworkVariable<int>();
    public NetworkVariable<int> unspentUnlocks = new NetworkVariable<int>();
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>();
    public NetworkVariable<int> Level = new NetworkVariable<int>();
    public NetworkVariable<float> XP = new NetworkVariable<float>();
    public NetworkVariable<int> Gold = new NetworkVariable<int>();


    public GameObject projectilePrefab;
    public NetworkVariable<int> teamNumber = new NetworkVariable<int>();
    public NetworkObject currentTarget; // current attack target
    [SerializeField] bool isAutoAttacking = false;
    public GameObject healthBarPrefab;
    public GameObject manaBarPrefab;
    public GameObject xpBarPrefab;
    public GameObject passiveCooldownBarPrefab;
    public GameObject abilty1CooldownBarPrefab;
    public GameObject abilty2CooldownBarPrefab;
    public GameObject UltimateCooldownBarPrefab;
    public GameObject attackDamagePrefab;
    public GameObject attackSpeedDisplayPrefab;
    public GameObject attackRangeDisplayPrefab;
    public GameObject armorDisplayPrefab;
    public GameObject cDRDisplayPrefab;
    public GameObject armorPenDisplayPrefab;
    public GameObject regenDisplayPrefab;
    public GameObject manaRegenDisplayPrefab;
    public GameObject goldDisplayPrefab;
    public GameObject moveSpeedDisplayPrefab;
    public GameObject shopPrefab;
    public GameObject unspentUpgradesPrefab;
    public GameObject scorePrefab;
    public GameObject deathScreenPrefab;
    public GameObject timerTextPrefab;
    public GameObject customTextPrefab;

    public GameObject enemyHealthBarPrefab;
    public GameObject HealthBar;

    private protected void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        lameManager = FindFirstObjectByType<LameManager>();
        bAM = FindFirstObjectByType<BryceAudioManager>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            BaseDamage.Value = attackDamage;
            BaseAttackSpeed.Value = autoAttackSpeed;
            BaseRange.Value = attackRange;
            BaseCDR.Value = cDR;
            BaseArmor.Value = health.armor;
            BaseArmorPen.Value = armorPen;
            BaseRegen.Value = regen;
            BaseManaRegen.Value = manaRegen;
            BaseSpeed.Value = maxSpeed;
            Gold.Value = 200;
        }
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0 && isDead.Value == false && IsServer)
            {
                isDead.Value = true;
                if (health.lastAttacker.TryGet(out NetworkObject attacker))
                {
                    if (attacker.tag == "Player")
                    {
                        attacker.GetComponent<BasePlayerController>().XP.Value += 50;
                        attacker.GetComponent<BasePlayerController>().Gold.Value += 50;
                        if (attacker.GetComponent<BasePlayerController>().DebtsDue.Value)
                        {
                            attacker.GetComponent<BasePlayerController>().Gold.Value += 100; 
                        }
                    }
                    else if (attacker.tag == "Puppet")
                    {
                        attacker = attacker.GetComponent<Puppet>().Father.GetComponent<NetworkObject>();
                        attacker.GetComponent<BasePlayerController>().XP.Value += 50;
                        attacker.GetComponent<BasePlayerController>().Gold.Value += 50;
                        if (attacker.GetComponent<BasePlayerController>().DebtsDue.Value)
                        {
                            attacker.GetComponent<BasePlayerController>().Gold.Value += 100;
                        }
                    }
                }
            }
        };
        XP.OnValueChanged += (float previousValue, float newValue) => //Checking for Level up
        {
            if (XP.Value >= XPToNextLevel.Value && IsServer)
            {
                LevelUpServerRPC();
            }
        };
        SuddenDeath.OnValueChanged += (bool previousValue, bool newValue) => 
        {
            if(SuddenDeath.Value)
            {
                currentTarget = null;
                if (teamNumber.Value == 1)
                {
                    transform.position = new Vector3(410, 70, 0);
                }
                else
                {
                    transform.position = new Vector3(440, 70, 0);
                }
                isDead.Value = false;
                health.currentHealth.Value = health.maxHealth.Value;
                mana = maxMana;
                attackDamage = BaseDamage.Value;
                autoAttackSpeed = BaseAttackSpeed.Value;
                attackRange = BaseRange.Value;
                cDR = BaseCDR.Value;
                health.armor = BaseArmor.Value;
                armorPen = BaseArmorPen.Value;
                regen = BaseRegen.Value;
                manaRegen = BaseManaRegen.Value;
                maxSpeed = BaseSpeed.Value;

                DamageBuff.Value = 0;
                AttackSpeedBuff.Value = 0;
                RangeBuff.Value = 0;
                CDRBuff.Value = 0;
                ArmorBuff.Value = 0;
                ArmorPenBuff.Value = 0;
                RegenBuff.Value = 0;
                ManaRegenBuff.Value = 0;
                SpeedBuff.Value = 0;
                health.markedValue = 1;
                isStunned.Value = false;
                appliesDarkness.Value = false;
                darknessDuration = 120;
                health.darknessEffect = false;

                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i] != null)
                    {
                        StopCoroutine(Buffs[i]);
                    }
                }
            }
        };
        isDead.OnValueChanged += (bool previousValue, bool newValue) => //Checking if dead
        {
            if (isDead.Value)
            {
                currentTarget = null;
                transform.position = new Vector3(10000, 10000, 0);
                StartCoroutine(lameManager.PlayerDeath(gameObject.GetComponent<NetworkObject>(), lameManager.respawnLength.Value, OwnerClientId));
                attackDamage = BaseDamage.Value;
                autoAttackSpeed = BaseAttackSpeed.Value;
                attackRange = BaseRange.Value;
                cDR = BaseCDR.Value;
                health.armor = BaseArmor.Value;
                armorPen = BaseArmorPen.Value;
                regen = BaseRegen.Value;
                manaRegen = BaseManaRegen.Value;
                maxSpeed = BaseSpeed.Value;

                DamageBuff.Value = 0;
                AttackSpeedBuff.Value = 0;
                RangeBuff.Value = 0;
                CDRBuff.Value = 0;
                ArmorBuff.Value = 0;
                ArmorPenBuff.Value = 0;
                RegenBuff.Value = 0;
                ManaRegenBuff.Value = 0;
                SpeedBuff.Value = 0;
                health.markedValue = 1;
                isStunned.Value = false;
                appliesDarkness.Value = false;
                darknessDuration = 120;
                health.darknessEffect = false;
                
                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i] != null)
                    {
                        StopCoroutine(Buffs[i]);
                    }
                }
            }
            else
            {
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
                GameObject shop = Instantiate(shopPrefab, playerCanvas.transform);
                shop.GetComponent<Shop>().enabled = true;

                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;

                GameObject manaBar = Instantiate(manaBarPrefab, playerCanvas.transform);
                manaBar.GetComponent<PlayerManaBar>().enabled = true;

                GameObject xpBar = Instantiate(xpBarPrefab, playerCanvas.transform);
                xpBar.GetComponent<PlayerXPBar>().enabled = true;

                GameObject abilty1CooldownBar = Instantiate(abilty1CooldownBarPrefab, playerCanvas.transform);
                abilty1CooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject abilty2CooldownBar = Instantiate(abilty2CooldownBarPrefab, playerCanvas.transform);
                abilty2CooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject ultimateCooldownBar = Instantiate(UltimateCooldownBarPrefab, playerCanvas.transform);
                ultimateCooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject AttackDisplay = Instantiate(attackDamagePrefab, playerCanvas.transform);
                AttackDisplay.GetComponent<PlayerDamageDisplay>().enabled = true;

                GameObject attackSpeedDisplay = Instantiate(attackSpeedDisplayPrefab, playerCanvas.transform);
                attackSpeedDisplay.GetComponent<PlayerAttackSpeedDisplay>().enabled = true;

                GameObject attackRangeDisplay = Instantiate(attackRangeDisplayPrefab, playerCanvas.transform);
                attackRangeDisplay.GetComponent<PlayerAttackRangeDisplay>().enabled = true;

                GameObject armorDisplay = Instantiate(armorDisplayPrefab, playerCanvas.transform);
                armorDisplay.GetComponent<PlayerArmorDisplay>().enabled = true;

                GameObject cDRDisplay = Instantiate(cDRDisplayPrefab, playerCanvas.transform);
                cDRDisplay.GetComponent<PlayerCDRDisplay>().enabled = true;

                GameObject armorPenDisplay = Instantiate(armorPenDisplayPrefab, playerCanvas.transform);
                armorPenDisplay.GetComponent<PlayerArmorPenDisplay>().enabled = true;

                GameObject moveSpeedDisplay = Instantiate(moveSpeedDisplayPrefab, playerCanvas.transform);
                moveSpeedDisplay.GetComponent<PlayerMoveSpeedDisplay>().enabled = true;

                GameObject regenDisplay = Instantiate(regenDisplayPrefab, playerCanvas.transform);
                regenDisplay.GetComponent<PlayerRegenDisplay>().enabled = true;

                GameObject manaRegenDisplay = Instantiate(manaRegenDisplayPrefab, playerCanvas.transform);
                manaRegenDisplay.GetComponent<PlayerManaRegenDisplay>().enabled = true;

                GameObject goldDisplay = Instantiate(goldDisplayPrefab, playerCanvas.transform);
                goldDisplay.GetComponent<PlayerGoldDisplay>().enabled = true;
                
                GameObject upgradesDisplay = Instantiate(unspentUpgradesPrefab, playerCanvas.transform);
                upgradesDisplay.GetComponent<PlayerUnspentUpgradsUI>().enabled = true;
                
                GameObject scoreDisplay = Instantiate(scorePrefab, playerCanvas.transform);
                //scoreDisplay.GetComponent<ScoreUI>().enabled = true;
                
                GameObject deathScreen = Instantiate(deathScreenPrefab, playerCanvas.transform);
                deathScreen.GetComponent<DeathScreen>().enabled = true;

                GameObject timerText = Instantiate(timerTextPrefab, playerCanvas.transform);
                timerText.GetComponent<MatchTimer>().enabled = true;

                GameObject customText = Instantiate(customTextPrefab, playerCanvas.transform);
                customText.GetComponent<CustomCharacterUI>().enabled = true;
            }
        }
        if (!IsOwner)
        {
            GameObject healthBar = Instantiate(enemyHealthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
            HealthBar = healthBar;
            healthBar.GetComponent<EnemyHealthBar>().enabled = true;
            healthBar.GetComponent<EnemyHealthBar>().SyncValues(gameObject, gameObject.transform, 3f);
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
        if (IsServer)
        {
            LevelUpServerRPC();
        }
    }

    private protected void Update()
    {
        SyncStats(); //Takes the base value of each stat, and adds whatever the current buff/debuff is
        if (!IsOwner) return;
        if(appliesDarkness.Value && darknessDuration > 0)
        {
            darknessDuration -= Time.deltaTime;
        } else if(appliesDarkness.Value && darknessDuration <= 0)
        {
            appliesDarkness.Value = false;
            darknessDuration = 120;
        }
        if (isDead.Value || isStunned.Value) return;
        // Get input and store it in playerInput
        if (!isAttacking)
        {
            Vector2 moveDir = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) moveDir.y = +1f;
            if (Input.GetKey(KeyCode.S)) moveDir.y = -1f;
            if (Input.GetKey(KeyCode.A))
            {
                moveDir.x = -1f;
                if (PlayerSprite != null)
                {
                    PlayerSprite.flipX = true;
                    if (IsServer)
                    {
                        FlipSpriteClientRpc(true);
                    }
                    else
                    {
                        FlipSpriteServerRpc(true);
                    }
                }
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveDir.x = +1f;
                if (PlayerSprite != null)
                {
                    PlayerSprite.flipX = false;
                    if (IsServer)
                    {
                        FlipSpriteClientRpc(false);
                    }
                    else
                    {
                        FlipSpriteServerRpc(false);
                    }
                }
            }
            playerInput = moveDir.normalized;
        }
        else if (isAttacking)
        {
            animator.SetBool("AutoAttack", true);
            playerInput = new Vector2(0, 0);
        }
        // Only cancel auto-attacking if player initiates movement with WASD
        if (playerInput.magnitude > 0)
        {
            currentTarget = null;
            isAutoAttacking = false;
        }
        if (currentTarget != null && currentTarget.GetComponent<BasePlayerController>() != null && currentTarget.GetComponent<BasePlayerController>().isDead.Value || currentTarget != null && isDead.Value)
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
        /*if (isAttacking)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= 1f / autoAttackSpeed)
            {
                isAttacking = false;
            }
        } */

        // Handle auto-attacking
        if (currentTarget != null && !isAttacking)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= 1f / autoAttackSpeed)
            {
                float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
                if (distanceToTarget <= attackRange)
                {
                    //animator.SetBool("AutoAttack", true);
                    isAttacking = true;
                    //PerformAutoAttack(currentTarget);
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
    private void PerformAutoAttack()
    {
        if (currentTarget == null || !isAttacking) return;

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget <= attackRange)
        {
            if (isMelee)
            {
                // melee damage
                DealDamageServerRpc(attackDamage, new NetworkObjectReference(currentTarget), new NetworkObjectReference(NetworkObject));
            }
            else
            {
                // Ranged attack with projectile
                SpawnProjectileServerRpc(new NetworkObjectReference(currentTarget), new NetworkObjectReference(NetworkObject));
            }
            isAttacking = false;
            lastAttackTime = Time.time;
        }
    }

    private void AutoAttackEnd()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
    }

    public void AbilityOneAnimation()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
        currentTarget = null;
        animator.SetBool("AbilityOne", true);
    }
    public void AbilityOneAnimationEnd()
    {
        animator.SetBool("AbilityOne", false);
    }

    public void AbilityTwoAnimation()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
        currentTarget = null;
        animator.SetBool("AbilityTwo", true);
    }
    public void AbilityTwoAnimationEnd()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
        currentTarget = null;
        animator.SetBool("AbilityTwo", false);
    }

    public void UltimateAnimation()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
        currentTarget = null;
        animator.SetBool("Ult", true);
    }
    public void UltimateAnimationEnd()
    {
        isAttacking = false;
        animator.SetBool("AutoAttack", false);
        currentTarget = null;
        animator.SetBool("Ult", false);
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
            if (charName == "Puppeteering")
            {
                bAM.PlayServerRpc("Puppeteering Auto", gameObject.transform.position);
                bAM.PlayClientRpc("Puppeteering Auto", gameObject.transform.position);
            }
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();

            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(autoAttackProjSpeed, attackDamage, targetObj, senderObj, armorPen);
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
            if(charName == "Decay")
            {
                bAM.PlayServerRpc("Decay Auto", gameObject.transform.position);
                bAM.PlayClientRpc("Decay Auto", gameObject.transform.position);
            }
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, armorPen, false);
            if(appliesDarkness.Value && target.GetComponent<Health>().darknessEffect == false)
            {
                health.InflictBuffServerRpc(new NetworkObjectReference(target), "Darkness", -1, 5, true);
            }
        }
        else
        {
            Debug.Log("BZZZZ wrong answer");
        }
    }
    #endregion
    private void FixedUpdate()
    {
        if (isDead.Value) return;

        if (resevoirRegen.Value == true)
        {
            if (IsServer)
            {
                health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + (health.maxHealth.Value * 0.01f), health.maxHealth.Value);
            }
            if (IsOwner)
            {
                mana = Mathf.Min(mana + (maxMana * 0.01f), maxMana);
            }
        }

        if (!IsOwner) return;

        animator.SetFloat("Speed", currentSpeed);


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
                    if (IsServer)
                    {
                        FlipSpriteClientRpc(currentTarget.transform.position.x < transform.position.x);
                    }
                    else
                    {
                        FlipSpriteServerRpc(currentTarget.transform.position.x < transform.position.x);
                    }
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

    [ServerRpc]
    public void FlipSpriteServerRpc(bool flip)
    {
        if (flip)
        {
            PlayerSprite.flipX = true;
        }
        else
        {
            PlayerSprite.flipX = false;
        }
    }

    [ClientRpc]
    public void FlipSpriteClientRpc(bool flip)
    {
        if (flip)
        {
            PlayerSprite.flipX = true;
        }
        else
        {
            PlayerSprite.flipX = false;
        }
    }

    public void SyncStats()
    {
        attackDamage = BaseDamage.Value + DamageBuff.Value;
        autoAttackSpeed = BaseAttackSpeed.Value + AttackSpeedBuff.Value;
        attackRange = BaseRange.Value + RangeBuff.Value;
        cDR = BaseCDR.Value + CDRBuff.Value;
        health.armor = BaseArmor.Value + ArmorBuff.Value;
        armorPen = BaseArmorPen.Value + ArmorPenBuff.Value;
        regen = BaseRegen.Value + RegenBuff.Value;
        manaRegen = BaseManaRegen.Value + ManaRegenBuff.Value;
        maxSpeed = BaseSpeed.Value + SpeedBuff.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration, bool hasDuration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Speed")
        {
            SpeedBuff.Value += amount;
        }
        if (buffType == "Attack Damage")
        {
            DamageBuff.Value += amount;
        }
        if (buffType == "Armor")
        {
            ArmorBuff.Value += amount;
        }
        if (buffType == "Armor Pen")
        {
            ArmorPenBuff.Value += amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            AttackSpeedBuff.Value += amount;
        }
        if (buffType == "Regen")
        {
            RegenBuff.Value += amount;
        }
        if (buffType == "Mana Regen")
        {
            ManaRegenBuff.Value += amount;
        }
        if (buffType == "Max Mana")
        {
            maxMana += amount;
        }
        if (buffType == "CDR")
        {
            CDRBuff.Value += amount;
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
            SpeedBuff.Value -= BaseSpeed.Value;
        }
        if (buffType == "Darkness")
        {
            SpeedBuff.Value += amount;
            health.darknessEffect = true;
        }
        if (buffType == "Stun")
        {
            isStunned.Value = true;
        }
        StatChangeClientRpc(buffType, amount);
        if (hasDuration)
        {
            IEnumerator coroutine = BuffDuration(buffType, amount, duration, Buffs.Count);
            StartCoroutine(coroutine);
            Buffs.Add(coroutine);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ItemEffectServerRpc(string buffType, float amount)
    {
        if(buffType == "Gold")
        {
            int value = Mathf.FloorToInt(amount);
            Gold.Value -= value;
        }
        if (buffType == "Speed")
        {
            BaseSpeed.Value += amount;
        }
        if (buffType == "Attack Damage")
        {
            BaseDamage.Value += amount;
        }
        if (buffType == "Armor")
        {
            BaseArmor.Value += amount;
        }
        if (buffType == "Armor Pen")
        {
            BaseArmorPen.Value += amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            BaseAttackSpeed.Value += amount;
        }
        if (buffType == "Regen")
        {
            BaseRegen.Value += amount;
        }
        if (buffType == "Mana Regen")
        {
            BaseManaRegen.Value += amount;
        }
        if (buffType == "Max Mana")
        {
            maxMana += amount;
        }
        if (buffType == "CDR")
        {
            BaseCDR.Value += amount;
        }
        if (buffType == "Health")
        {
            health.maxHealth.Value += amount;
        }
        if (buffType == "Player Kill Money")
        {
           DebtsDue.Value = true;
        }
        if (buffType == "Lifesteal")
        {
            LifeStealPercentage.Value += amount;
        }
    }
    public IEnumerator BuffDuration(string buffType, float amount, float duration, int buffNumber) //Waits a bit before changing stats back to default
    {
        yield return new WaitForSeconds(duration);
        BuffEndServerRpc(buffType, amount, duration);
        for (int i = 0; i < Buffs.Count; i++)
        {
            if (i == buffNumber)
            {
                Buffs.Remove(Buffs[i]);
            }
        }
        //Buffs.Remove(BuffDuration(buffType, amount, duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuffEndServerRpc(string buffType, float amount, float duration) //changes stat to what it was before
    {
        if (buffType == "Speed")
        {
            SpeedBuff.Value -= amount;
        }
        if (buffType == "Attack Damage")
        {
            DamageBuff.Value -= amount;
        }
        if (buffType == "Armor")
        {
            ArmorBuff.Value -= amount;
        }
        if (buffType == "Armor Pen")
        {
            ArmorPenBuff.Value -= amount;
        }
        if (buffType == "Auto Attack Speed")
        {
            AttackSpeedBuff.Value -= amount;
        }
        if (buffType == "Regen")
        {
            RegenBuff.Value -= amount;
        }
        if (buffType == "Mana Regen")
        {
            ManaRegenBuff.Value -= amount;
        }
        if (buffType == "Max Mana")
        {
            maxMana -= amount;
        }
        if (buffType == "CDR")
        {
            CDRBuff.Value -= amount;
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
            SpeedBuff.Value += BaseSpeed.Value;
        }
        if (buffType == "Darkness")
        {
            SpeedBuff.Value -= amount;
            health.darknessEffect = false;
        }
        if (buffType == "Stun")
        {
            isStunned.Value = false;
        }
        StatChangeClientRpc(buffType, -amount);
    }

    [Rpc(SendTo.NotServer)]
    public void StatChangeClientRpc(string buffType, float amount) //This makes sure that stats are synced to client and host
    {
        if (buffType == "Max Mana")
        {
            maxMana += amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LevelUpServerRPC()
    {
        Level.Value++;
        if (Level.Value > 1)
        {
            unspentUpgrades.Value++;
            BaseSpeed.Value += statGrowthRate[0];
            BaseDamage.Value += statGrowthRate[1];
            BaseArmor.Value += statGrowthRate[2];
            BaseArmorPen.Value += statGrowthRate[3];
            BaseAttackSpeed.Value += statGrowthRate[4];
            BaseRegen.Value += statGrowthRate[5];
            BaseManaRegen.Value += statGrowthRate[6];
            TriggerBuffServerRpc("Max Mana", statGrowthRate[7], 0, false);
            BaseCDR.Value += statGrowthRate[8];
            TriggerBuffServerRpc("Health", statGrowthRate[9], 0, false);
            XP.Value = XP.Value - XPToNextLevel.Value;
        }
        else
        {
            XP.Value = 0;
        }
        XPToNextLevel.Value = XPPerLevel[Level.Value];
        if(Level.Value < 4)
        {
            unspentUnlocks.Value++;
        } else
        {
            unspentUpgrades.Value++;
        }
    }
}