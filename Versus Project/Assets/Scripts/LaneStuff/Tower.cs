using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class Tower : NetworkBehaviour
{
    public int Team;
    protected private LameManager lameManager;
    public BryceAudioManager bAM;

    public Transform towerTarget;
    public Transform enemyMinionTarget;

    public Animator animator;

    public bool isAttacking = false;
    public bool cooldown = false;
    public bool aggro = false;
    public bool dead;
    public bool playerLastHit = false;

    public GameObject tower;
    public GameObject enemyPlayer;
    public GameObject enemyPuppet;
    public GameObject enemyMinion;
    public GameObject projectilePrefab;
    public NetworkObject networkTower;
    public NetworkObject currentTarget;

    private Vector3 distanceFromTarget;
    private Vector3 distanceFromPlayer;
    public Vector3 distanceFromPuppet;
    private Vector3 distanceFromMinion;
    private Vector3 oldTarget;

    public float goldRange;
    public int goldGiven;

    public float Damage;
    public float armorPen = 1;
    public int orderInLane;
    public float aggroTimer = 0f;
    public float aggroLength = 10f;
    public float cooldownLength = 2f;
    public float cooldownTimer = 0f;
    public float towerRange = 10f;
    public Health health;

    public GameObject healthBarPrefab;
    public GameObject HealthBar;

    void Start()
    {
        networkTower = tower.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
        bAM = FindFirstObjectByType<BryceAudioManager>();
    }

    protected virtual void Update()
    {
        if (!IsServer || isAttacking) return;
        if (Team == 1)
        {
            if (lameManager.teamOneTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            if (lameManager.playerTwoChar != null)
            {
                enemyPlayer = lameManager.playerTwoChar;
            }
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
        else if (Team == 2)
        {
            if (lameManager.teamTwoTowersLeft.Value == orderInLane)
            {
                health.invulnerable = false;
            }
            if (lameManager.playerOneChar != null)
            {
                enemyPlayer = lameManager.playerOneChar;
            }
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
        if (enemyPlayer != null)
        {
            distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPlayer.transform.position.x, towerTarget.position.y - enemyPlayer.transform.position.y, 0);
        }
        else
        {
            distanceFromPlayer = new Vector3(1000, 1000, 1000);
        }
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
            distanceFromMinion = new Vector3(1000, 1000, 0);
        }
        if ((distanceFromMinion.magnitude < distanceFromPlayer.magnitude && aggro == false && enemyMinion != null) || (distanceFromMinion.magnitude < towerRange && aggro == false && enemyMinion != null))
        {
            distanceFromTarget = distanceFromMinion;
            currentTarget = enemyMinion.GetComponent<NetworkObject>();
        }
        else if (distanceFromPlayer.magnitude < distanceFromMinion.magnitude)
        {
            if (enemyPlayer.GetComponent<PuppeteeringPlayerController>() != null && enemyPlayer.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
            {
                oldTarget = new Vector3(1000, 1000, 0);
                foreach (GameObject puppet in enemyPlayer.GetComponent<PuppeteeringPlayerController>().PuppetList)
                {
                    Vector3 directionToPuppet = new Vector3(towerTarget.position.x - puppet.transform.position.x, towerTarget.position.y - puppet.transform.position.y, 0);
                    if (oldTarget.magnitude > directionToPuppet.magnitude)
                    {
                        oldTarget = directionToPuppet;
                        distanceFromPuppet = directionToPuppet;
                        enemyPuppet = puppet;
                    }
                }
            }
            if (distanceFromPlayer.magnitude > distanceFromPuppet.magnitude && enemyPlayer.GetComponent<PuppeteeringPlayerController>() != null && enemyPlayer.GetComponent<PuppeteeringPlayerController>().puppetsAlive.Value > 0)
            {
                distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPuppet.transform.position.x, towerTarget.position.y - enemyPuppet.transform.position.y, 0);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPuppet.GetComponent<NetworkObject>();
            }
            else
            {
                distanceFromPlayer = new Vector3(towerTarget.position.x - enemyPlayer.transform.position.x, towerTarget.position.y - enemyPlayer.transform.position.y, 0);
                distanceFromTarget = distanceFromPlayer;
                currentTarget = enemyPlayer.GetComponent<NetworkObject>();
            }
        }
        if (distanceFromTarget.magnitude < towerRange && cooldown == false && currentTarget != null)
        {
            isAttacking = true;
            animator.SetBool("Attacking", isAttacking);
            if (currentTarget.tag == "Player" || currentTarget.tag == "Puppet")
            {
                bAM.PlayServerRpc("Tower Alert", tower.transform.position);
                bAM.PlayClientRpc("Tower Alert", tower.transform.position);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.lastAttacker.TryGet(out NetworkObject attacker))
            {
                if (attacker.tag == "Player")
                {
                    playerLastHit = true;
                }
                else
                {
                    playerLastHit = false;
                }
            }
            if (health.currentHealth.Value <= 0 && IsServer == true && dead == false)
            {
                dead = true;
                bAM.PlayServerRpc("Tower Break", tower.transform.position);
                bAM.PlayClientRpc("Tower Break", tower.transform.position);
                if (distanceFromPlayer.magnitude < goldRange)
                {
                    enemyPlayer.GetComponent<BasePlayerController>().Gold.Value += goldGiven;
                }
                lameManager.TowerDestroyedServerRPC(Team);
                tower.GetComponent<NetworkObject>().Despawn();
            }
        };
        health.invulnerable = true;
        GameObject healthBar = Instantiate(healthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
        HealthBar = healthBar;
        healthBar.GetComponent<EnemyHealthBar>().enabled = true;
        healthBar.GetComponent<EnemyHealthBar>().SyncValues(tower, towerTarget, 3f);
    }

    public void DealDamage()
    {
        if (currentTarget != null)
        {
            if (currentTarget.GetComponent<MeleeMinion>() != null)
            {
                float minionDamage = currentTarget.GetComponent<MeleeMinion>().health.maxHealth.Value * 0.75f;
                Debug.Log(minionDamage);
                SpawnProjectileServerRpc(minionDamage, currentTarget, networkTower);

            }
            else
            {
                SpawnProjectileServerRpc(Damage, currentTarget, networkTower);
            }
        }
        else
        {
            Debug.Log("erm");
            isAttacking = false;
            animator.SetBool("Attacking", isAttacking);
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
            controller.Initialize(15, damage, targetObj, senderObj, armorPen);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBuffServerRpc(string buffType, float amount, float duration) //this takes a stat, then lowers/increase it, and triggers a timer to set it back to default
    {
        if (buffType == "Attack Damage")
        {
            Damage += amount;
            if (Damage <= 1f)
            {
                amount = -Damage + 1f + amount;
                Damage = 1f;
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
            cooldownLength -= amount;
            if (cooldownLength <= 0.1f)
            {
                amount = -cooldownLength + 0.1f + amount;
                cooldownLength = 0.1f;
            }
        }
        if (buffType == "Marked")
        {
            health.markedValue += amount;
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
        if (buffType == "Attack Damage")
        {
            Damage -= amount;
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
            cooldownLength += amount;
        }
        if (buffType == "Marked")
        {
            health.markedValue -= amount;
        }
    }
}
