using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullHealEffect : LevelUpEffect
{
    public playerController playerController;

    public override void ApplyEffect()
    {
        GameObject player = GameObject.FindWithTag("Player");

        
        if (player != null)
        {
            playerController = player.GetComponent<playerController>();
            
            if (playerController != null)
            {
                playerController.health.currentHealth = playerController.health.maxHealth;
                playerController.OnTakeDamage();
                playerController.pauseMenu.SetActive(true);
            }
            else
            {
                Debug.LogWarning("PlayerController killed its self");
            }
        }
        else
        {
            Debug.LogWarning("Player killed its self");
        }
    }
}
