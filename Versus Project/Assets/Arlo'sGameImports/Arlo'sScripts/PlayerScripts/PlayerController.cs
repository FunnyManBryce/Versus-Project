using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class playerController : MonoBehaviour
{
    private Rigidbody2D rb2d;
    public Animator animator;
    [SerializeField] SpriteRenderer PlayerSprite;

    public float maxSpeed = 2, acceleration = 50, deacceleration = 100;
    [SerializeField] 
    private float currentSpeed = 0;
    public float dashSpeed = 30f;
    public bool canDash = true;

    public float dashLength = 0.5f, dashCooldown = 1f;

    public bool isDashing;
    private float dashTimer;

    public float currentExperience;
    public float maxExperience;
    public int currentLevel;

    private Vector2 oldMovementInput;
    public Vector2 playerInput { get; set; }

    public Health health;
    public HealthBar healthBar;
    public GameObject pauseMenu;
    public GameObject levelUpMenu;

    //public LevelUpMenu levelUpMenuScript;
    //stuff I wont change
    #region
    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }
    private void OnDisable()
    {
        ExperienceManager.Instance.OnExperienceChange -= HandleExperiencChange;
    }

    private void Start()
    {
        OnTakeDamage();
        playerInput = transform.position;
        healthBar.SetMaxHealth(health.maxHealth);
        ExperienceManager.Instance.OnExperienceChange += HandleExperiencChange;
    }

    private void FixedUpdate()
    {

        animator.SetBool("IsDashing", isDashing);
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("CanDash", canDash);
        if (!isDashing)
        {
            if (playerInput.magnitude > 0 && currentSpeed >= 0)
            {
                oldMovementInput = playerInput;
                currentSpeed += acceleration * maxSpeed * Time.fixedDeltaTime;
            }
            else
            {
                currentSpeed -= deacceleration * maxSpeed * Time.fixedDeltaTime;
            }
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
            rb2d.velocity = oldMovementInput * currentSpeed;
        }
        else
        {
            Dash();
        }
    }
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (PlayerSprite != null)
            {
                PlayerSprite.flipX = true;
            }
        } else if (Input.GetKeyDown(KeyCode.D))
        {
            if (PlayerSprite != null)
            {
                PlayerSprite.flipX = false;
            }
        }
    }
    #endregion   //

    //dash shit when I figure that out
    #region
    public void PerformDash()
    {
        isDashing = true;
        //Set the length of the dash to the how fair the dash would go
        dashTimer = dashLength;
    }
    private void Dash()
    {
        if (canDash)
        {
            FindObjectOfType<BryceAudioManager>().Play("Dash");
            rb2d.velocity = oldMovementInput.normalized * dashSpeed;
            dashTimer -= Time.deltaTime;
            canDash = false;
            health.DashInvincibility();
        }
        else
        {
            return;
        }
    }


    public void DashReset()
    {
        Debug.Log("why?");
        isDashing = false;
    }

    public void CanDashReset()
    {
        Debug.Log("WHAT");
        if (!canDash)
        {
            canDash = true;
        }
    }

    #endregion

    private void HandleExperiencChange(int newExperience)
    {
        currentExperience += newExperience;
        if (currentExperience >= maxExperience)
        {
            currentExperience -= maxExperience;
            LevelUp();
            currentLevel++;
        }
    }

    private void LevelUp()
    {
        FindObjectOfType<BryceAudioManager>().Play("Level Up");
        Time.timeScale = 0;
        pauseMenu.SetActive(false); //possibly cause of pause menu issue on level up
        levelUpMenu.SetActive(true);
        health.maxHealth += 20;
        health.currentHealth += 70;
        if (health.currentHealth > health.maxHealth)
        {
            health.currentHealth = health.maxHealth;
        }
        maxExperience += 75;
        OnTakeDamage();
    }

    public void OnTakeDamage() 
    {
        healthBar.SetMaxHealth(health.maxHealth);
        healthBar.SetHealth(health.currentHealth);
    }
}
