using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int currentHealth, maxHealth;

    public UnityEvent<GameObject> OnHitWithRefrence, OnDeathWithRefrence;

    [SerializeField] private bool isDead = false;
    public bool isInvincible = false;
    public float invincibilityDuration = 1.5f;

    public PlayerBlinkFeedback invincibilityFeedback;

    [SerializeField] playerController player;
    public void InitializeHealth(int healthValue)
    {
        currentHealth = healthValue;
        maxHealth = healthValue;
        isDead = false;
    }

    public void GetHit(int amount, GameObject sender)
    {
        if (isDead)
            return;
        if (sender.layer == gameObject.layer)
            return;
        if (isInvincible)
            return;
        currentHealth -= amount;
        if (currentHealth > 0)
        {
            OnHitWithRefrence?.Invoke(sender);

            int playerLayer = LayerMask.NameToLayer("Player");
            
            if (sender.layer != playerLayer)
            {
                StartCoroutine(StartInvicibilty());
            }
            if(player != null)
            {
                player.OnTakeDamage();

            }
        }
        else
        {
            OnDeathWithRefrence?.Invoke(sender);
            isDead = true;
            Destroy(gameObject);
        }
    }
    public IEnumerator StartInvicibilty() 
    {
        isInvincible = true;

        invincibilityFeedback?.PlayBlinkFeedback();

        yield return new WaitForSeconds(invincibilityDuration);

        isInvincible = false;
    }
    public void DashInvincibility()
    {
        StartCoroutine(Dashing());
    }
    public IEnumerator Dashing()
    {
        isInvincible = true;

        invincibilityFeedback?.PlayBlinkFeedback();

        yield return new WaitForSeconds(0.2f + invincibilityDuration/6);

        isInvincible = false;
    }
}
