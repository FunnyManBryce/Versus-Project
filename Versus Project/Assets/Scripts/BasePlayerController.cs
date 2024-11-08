using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class BasePlayerController : NetworkBehaviour
{
    private Rigidbody2D rb2d;
    public Animator animator;
    [SerializeField] SpriteRenderer PlayerSprite;

    public float maxSpeed = 2, acceleration = 50, deacceleration = 100;
    [SerializeField]
    private float currentSpeed = 0;

    private Vector2 movementInput;
    private Vector2 playerInput;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        playerInput = transform.position;
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
}
