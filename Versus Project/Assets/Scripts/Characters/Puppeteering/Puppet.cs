using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Puppet : NetworkBehaviour
{
    public int Team;

    public Health health;
    private LameManager lameManager;

    public Transform puppetPos;
    public Transform jungleTarget;

    public NavMeshAgent agent;

    public Animator animator;
    [SerializeField] SpriteRenderer puppetSprite;
    public RuntimeAnimatorController OffensiveAnimator;
    public RuntimeAnimatorController DefensiveAnimator;

    private Vector3 distanceFromFather;
    public Vector3 distanceFromTarget;
    private Vector3 oldTarget;

    public bool defensiveMode;
    public bool isAttacking = false;
    public bool isChasing = false;
    public bool cooldown = false;
    public bool dead;
    private bool foundTarget;
    public float regen = 5f;
    private float lastRegenTick = 0f;
    public bool isStunned;

    public string targetName;
    public float Damage;
    public float armorPen;
    public float lifestealMultiplier = 0f;
    public float followDistance = 10;
    public float stopFollowDistance = 5;

    public float attackDistance = 3;
    public float moveSpeed = 3;
    public float cooldownLength = 0.5f;
    public float cooldownTimer = 0f;

    public GameObject Father;
    public GameObject jungleEnemy;
    public GameObject enemyTarget;
    public GameObject puppet;
    public GameObject projectilePrefab;
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

    public void SyncStats()
    {
        var FatherScript = Father.GetComponent<PuppeteeringPlayerController>();
        Damage = 1f * FatherScript.attackDamage;
        moveSpeed = FatherScript.puppetSpeedMultiplier * FatherScript.maxSpeed;
        cooldownLength = FatherScript.puppetCooldown;
        lifestealMultiplier = FatherScript.lifestealMultiplier;
        regen = FatherScript.puppetRegen;
        if(FatherScript.Level.Value < 1)
        {
            health.maxHealth.Value = FatherScript.puppetStartingHealth + 50;
        }
        else if (FatherScript.Level.Value >= 1)
        {
            health.maxHealth.Value = FatherScript.puppetStartingHealth + (50 * FatherScript.Level.Value);
        }
    }

    [ClientRpc]
    public void FlipSpriteClientRpc(bool flip)
    {
        if (flip)
        {
            puppetSprite.flipX = true;
        }
        else
        {
            puppetSprite.flipX = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking && cooldown)
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
        if(isStunned)
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
        }
        if(!IsServer || isStunned) return;
        if (agent.desiredVelocity.x < 0)
        {
            puppetSprite.flipX = true;
            FlipSpriteClientRpc(true);
        }
        else if(agent.desiredVelocity.x > 0)
        {
            puppetSprite.flipX = false;
            FlipSpriteClientRpc(false);
        }
        if (isAttacking) return;
        if (agent.desiredVelocity.magnitude > 0.1)
        {
            animator.SetFloat("Speed", 1);
        } else
        {
            animator.SetFloat("Speed", 0);
        }
        SyncStats();
        AnimChangeClientRpc(defensiveMode);
        if (defensiveMode == true)
        {
            animator.runtimeAnimatorController = DefensiveAnimator;
            agent.SetDestination(Father.transform.position);
            attackDistance = 12;
            float currentTime = Time.time;
            if (currentTime - lastRegenTick >= 1f) // Check every second
            {
                RegenHealthServerRpc(regen);
                lastRegenTick = currentTime;
            }
        }
        else
        {
            animator.runtimeAnimatorController = OffensiveAnimator;
            attackDistance = 4;
        }
        if (Father.GetComponent<PuppeteeringPlayerController>().currentTarget != null)
        {
            enemyTarget = Father.GetComponent<PuppeteeringPlayerController>().currentTarget.gameObject;
            distanceFromTarget = new Vector3(puppetPos.position.x - enemyTarget.transform.position.x, puppetPos.position.y - enemyTarget.transform.position.y, 0); ;
        }
        else
        {
            oldTarget = new Vector3(1000, 1000, 0);
            Vector2 pos = new Vector2(Father.transform.position.x, Father.transform.position.y);
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 12f);
            foreach (var collider in hitColliders)
            {
                if (collider.GetComponent<Health>() != null && collider != puppet.GetComponent<Collider2D>() && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger == false)
                {
                    foundTarget = true;
                    GameObject potentialTarget = collider.gameObject;
                    Vector3 directionToTarget = new Vector3(puppetPos.position.x - potentialTarget.transform.position.x, puppetPos.position.y - potentialTarget.transform.position.y, 0);
                    if (oldTarget.magnitude > directionToTarget.magnitude)
                    {
                        oldTarget = directionToTarget;
                        distanceFromTarget = directionToTarget;
                        enemyTarget = potentialTarget;
                    }
                }
            }
            if(!foundTarget)
            {
                distanceFromTarget = oldTarget;
                enemyTarget = null;
            }
            foundTarget = false;
        }
        if (cooldown == true && cooldownTimer < cooldownLength)
        {
            cooldownTimer += Time.deltaTime;
        }
        else if (cooldownTimer >= cooldownLength)
        {
            cooldown = false;
            cooldownTimer = 0;
        }
        distanceFromFather = new Vector3(puppetPos.position.x - Father.transform.position.x, puppetPos.position.y - Father.transform.position.y, 0);
        if (enemyTarget != null && distanceFromFather.magnitude < followDistance)
        {
            agent.speed = moveSpeed;
            currentTarget = enemyTarget.GetComponent<NetworkObject>();
            if (!defensiveMode)
            {
                agent.SetDestination(currentTarget.transform.position);
            }
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }
        if (enemyTarget != null && distanceFromTarget.magnitude < attackDistance && cooldown == false)
        {
            isAttacking = true;
            agent.speed = moveSpeed;
            animator.SetBool("Attacking", isAttacking);
            //DealDamage();
        }
        if (distanceFromFather.magnitude > followDistance)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(Father.transform.position);
        }
        else if (distanceFromFather.magnitude < 4 && isChasing == false && !defensiveMode)
        {
            agent.SetDestination(puppetPos.position);
        }
    }
    public void DealDamage()
    {
        if (currentTarget != null)
        {
            if (defensiveMode)
            {
                SpawnProjectileServerRpc(Damage, new NetworkObjectReference(currentTarget), new NetworkObjectReference(NetworkObject));
            }
            else
            {
                DealDamageServerRPC(Damage, currentTarget, puppet);
            }
        }
        else
        {
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
            cooldown = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileServerRpc(float damage, NetworkObjectReference target, NetworkObjectReference sender)
    {
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
        if (target.TryGet(out NetworkObject targetObj) && sender.TryGet(out NetworkObject senderObj))
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            netObj.Spawn();

            ProjectileController controller = projectile.GetComponent<ProjectileController>();
            controller.Initialize(10, damage / 2, targetObj, senderObj, armorPen);
        }
    }

    [Rpc(SendTo.Server)]
    public void DealDamageServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        isAttacking = false;
        animator.SetBool("Attacking", isAttacking);
        cooldown = true;
        if (reference.TryGet(out NetworkObject target))
        {
            target.GetComponent<Health>().TakeDamageServerRPC(damage, sender, armorPen, false);
            if (defensiveMode == false) //offensive mode provides lifesteal to player
            {
                Father.GetComponent<Health>().currentHealth.Value = Mathf.Min(Father.GetComponent<Health>().currentHealth.Value + (damage * lifestealMultiplier), Father.GetComponent<Health>().maxHealth.Value);
            }
        }
        else
        {
            Debug.Log("This is bad");
        }
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0)
            {
                if (IsServer == true && dead == false)
                {
                    dead = true;
                    Father.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value--;
                    Father.GetComponent<PuppeteeringPlayerController>().PuppetList.Remove(gameObject);
                    Father.GetComponent<PuppeteeringPlayerController>().puppetDeathTime.Value = lameManager.matchTimer.Value;
                    puppet.GetComponent<NetworkObject>().Despawn();
                }
            }
        };
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(puppet, puppetPos, 1.5f);
    }
    
    [ClientRpc]
    public void AnimChangeClientRpc(bool defensiveMode)
    {
        if(defensiveMode)
        {
            animator.runtimeAnimatorController = DefensiveAnimator;
        }
        else
        {
            animator.runtimeAnimatorController = OffensiveAnimator;
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

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Speed")
        {
            moveSpeed += amount;
            if (moveSpeed <= 0.5f)
            {
                amount = -moveSpeed + 0.5f + amount;
                moveSpeed = 0.5f;
            }
        }
        if (buffType == "Attack Damage")
        {
            Damage += amount;
            if (Damage <= 1f)
            {
                amount = -Damage + 1f + amount;
                Damage = 1f;
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
        if (buffType == "Armor")
        {
            health.armor += amount;
            if (health.armor <= 1f)
            {
                amount = -health.armor + 1f + amount;
                health.armor = 1f;
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
        if (buffType == "Marked")
        {
            health.markedValue += amount;
        }
        if (buffType == "Immobilized")
        {
            amount = moveSpeed;
            moveSpeed = 0;
        }
        if (buffType == "Invulnerable")
        {
            health.invulnerable = true;
        }
        if(buffType == "Darkness")
        {
            moveSpeed += amount;
            health.darknessEffect = true;
        }
        if (buffType == "Stun")
        {
            isStunned = true;
        }
        IEnumerator coroutine = BuffDuration(buffType, amount, duration);
        StartCoroutine(coroutine);
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
            moveSpeed -= amount;
        }
        if (buffType == "Attack Damage")
        {
            Damage -= amount;
        }
        if (buffType == "Armor Pen")
        {
            armorPen -= amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue -= amount;
        }
        if (buffType == "Immobilized")
        {
            moveSpeed += amount;
        }
        if (buffType == "Armor")
        {
            health.armor -= amount;
        }
        if (buffType == "Regen")
        {
            regen -= amount;
        }
        if (buffType == "Invulnerable")
        {
            health.invulnerable = false;
        }
        if (buffType == "Darkness")
        {
            moveSpeed -= amount;
            health.darknessEffect = false;
        }
        if (buffType == "Stun")
        {
            isStunned = false;
        }
    }

    [ServerRpc]
    private void RegenHealthServerRpc(float regenAmount)
    {
        health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + regenAmount, health.maxHealth.Value);
    }
}

