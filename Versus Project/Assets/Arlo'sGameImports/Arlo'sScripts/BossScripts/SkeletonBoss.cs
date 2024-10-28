using UnityEngine;

public class SkeletonBoss : MonoBehaviour
{
    public Animator animator;

    public GameObject player;
    public GameObject projectile;
    public GameObject boss;
    public GameObject summon;

    public SkeletonBossProjectile skeletonBossProjectile;

    public Health health;

    public int damage = 1;
    public int shotsFiredPerPhase = 4;
    int currentShotsFired = 0;
    int currentPhase = 1;
    public int maxSummons;
    public int currentSummons;

    float currentTimeBetweenShots;
    public float maxTimeBetweenShots = 3;
    float attackDelay = 0.75f;
    public float attackDelayStart = 0.75f;
    public float moveSpeed = 5;
    public float ramSpeed = 0.1f;
    float currentStunLength = 2.75f;
    public float totalStunLength;

    public Transform bossTarget;
    public Transform playerTarget;
    public Transform summonPositionOne;
    public Transform summonPositionTwo;

    bool bossStarted = false;
    bool movingStart = false;
    bool moving = false;
    bool ramming = false;
    bool shooting = false;
    bool doneFiring = false;
    bool summoning = true;
    bool stunned = false;
    public bool AttackAnimation;

    Vector2 bossPosition;
    Vector2 playerPosition;
    Vector2 distanceFromPlayer;
    static public Vector3 projectileRotation;

    [SerializeField] int expAmount = 100;
    private Vector2 randomDir;

    // Start is called before the first frame update
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerTarget = player.transform;
        skeletonBossProjectile = projectile.GetComponent<SkeletonBossProjectile>();
        bossStarted = true;
        
    }

    // Update is called once per frame
    private void Update()
    {
        animator.SetBool("Firing", AttackAnimation);
        animator.SetBool("Stunned", stunned);
        animator.SetBool("Charging", ramming);
        if (player != null)
        {
            if (ramming == false)
            {
                bossPosition = new Vector2(bossTarget.position.x, bossTarget.position.y);
                playerPosition = new Vector2(playerTarget.position.x, playerTarget.position.y);
                distanceFromPlayer = new Vector2(bossPosition.x - playerPosition.x, bossPosition.y - playerPosition.y);
            }
            if (health.currentHealth <= 150)
            {
                currentPhase = 2;
            }
            if (movingStart == true)
            {
                randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                moving = true;
                movingStart = false;
            }
            if (bossStarted == true)
            {
                if (currentTimeBetweenShots > 0 && ramming == false && summoning == false && stunned == false)
                {
                    currentTimeBetweenShots -= Time.deltaTime;
                }
                else if (currentTimeBetweenShots < 0)
                {
                    currentTimeBetweenShots = maxTimeBetweenShots;
                    shooting = true;
                }
                if (moving == true)
                {
                    transform.Translate(randomDir * Time.deltaTime * moveSpeed);
                }
                if (shooting == true)
                {
                    AttackAnimation = true;
                    if (attackDelay > 0.25)
                    {
                        
                        moveSpeed = 0f;
                        attackDelay -= Time.deltaTime;
                    }
                    else if (attackDelay > 0)
                    {

                        if (doneFiring == false)
                        {
                            FindObjectOfType<BryceAudioManager>().Play("Magma");
                            Debug.Log("firing");
                            projectileRotation = new Vector3(0, 0, 90);
                            Quaternion newRotation = Quaternion.Euler(projectileRotation);
                            Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                            projectileRotation = new Vector3(0, 0, -90);
                            newRotation = Quaternion.Euler(projectileRotation);
                            Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                            projectileRotation = new Vector3(0, 0, 0);
                            newRotation = Quaternion.Euler(projectileRotation);
                            Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                            projectileRotation = new Vector3(0, 0, 180);
                            newRotation = Quaternion.Euler(projectileRotation);
                            Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                            if (currentPhase == 2)
                            {
                                projectileRotation = new Vector3(0, 0, 45);
                                newRotation = Quaternion.Euler(projectileRotation);
                                Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                                projectileRotation = new Vector3(0, 0, -45);
                                newRotation = Quaternion.Euler(projectileRotation);
                                Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                                projectileRotation = new Vector3(0, 0, 135);
                                newRotation = Quaternion.Euler(projectileRotation);
                                Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                                projectileRotation = new Vector3(0, 0, -135);
                                newRotation = Quaternion.Euler(projectileRotation);
                                Instantiate(projectile, new Vector3(bossTarget.position.x, bossTarget.position.y), newRotation);
                            }
                            doneFiring = true;
                        }
                        attackDelay -= Time.deltaTime;
                    }
                    else if (attackDelay < 0)
                    {
                        AttackAnimation = false;
                        shooting = false;
                        attackDelay = attackDelayStart;
                        currentTimeBetweenShots = maxTimeBetweenShots;
                        if (currentPhase == 1)
                        {
                            moveSpeed = 5;
                        }
                        else if (currentPhase == 2)
                        {
                            moveSpeed = 7;
                        }
                        currentShotsFired++;
                        doneFiring = false;
                        if (currentShotsFired == shotsFiredPerPhase)
                        {
                            currentShotsFired = 0;
                            rammingStart();
                            FindObjectOfType<BryceAudioManager>().Play("Skele Dash");
                        }
                    }
                }
                if (ramming == true)
                {
                    transform.Translate(-distanceFromPlayer * Time.deltaTime * ramSpeed);
                }
                if (stunned == true)
                {
                    if (currentStunLength > 0)
                    {
                        currentStunLength -= Time.deltaTime;
                    }
                    else if (currentStunLength <= 0)
                    {
                        currentStunLength = totalStunLength;
                        stunned = false;
                        summoning = true;
                    }
                }
                if (summoning == true)
                {
                    if (attackDelay > 0.25)
                    {
                        moveSpeed = 0f;
                        attackDelay -= Time.deltaTime;
                    }
                    else if (attackDelay > 0)
                    {
                        if (doneFiring == false)
                        {
                            if(currentSummons < maxSummons)
                            {
                                FindObjectOfType<BryceAudioManager>().Play("Boss Summon");
                                GameObject skeleton1 = Instantiate(summon, new Vector3(summonPositionOne.position.x, summonPositionOne.position.y), Quaternion.identity);
                                skeleton1.GetComponent<Enemy>().Creator = boss;
                                ArenaManager.enemiesAlive++;
                                currentSummons++;
                                if (currentPhase == 2 && currentSummons < 4)
                                {
                                    GameObject skeleton2 = Instantiate(summon, new Vector3(summonPositionTwo.position.x, summonPositionTwo.position.y), Quaternion.identity);
                                    skeleton2.GetComponent<Enemy>().Creator = boss;
                                    ArenaManager.enemiesAlive++;
                                    currentSummons++;
                                }
                            }
                            
                            doneFiring = true;
                        }
                        attackDelay -= Time.deltaTime;
                    }
                    else if (attackDelay < 0)
                    {
                        summoning = false;
                        attackDelay = attackDelayStart;
                        currentTimeBetweenShots = maxTimeBetweenShots;
                        if (currentPhase == 1)
                        {
                            moveSpeed = 5;
                        }
                        else if (currentPhase == 2)
                        {
                            moveSpeed = 7;
                        }
                        doneFiring = false;
                        movingStart = true;
                    }
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.tag == "Wall")
        {
            randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            if (ramming == true)
            {
                FindObjectOfType<BryceAudioManager>().Play("Skele Thud");
                stunned = true;
                ramming = false;
            }
        }
        if (col.gameObject.tag == "Player")
        {
            Health health;
            health = col.gameObject.GetComponent<Health>();
            health.GetHit(damage, boss);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "Wall")
        {
            randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }
    }

    private void rammingStart()
    {
        moving = false;
        ramming = true;
    }

    public void BossDeath()
    {
        ExperienceManager.Instance.AddExperience(expAmount);
        ArenaManager.enemiesAlive--;
        Debug.Log(ArenaManager.enemiesAlive);
    }

    
}