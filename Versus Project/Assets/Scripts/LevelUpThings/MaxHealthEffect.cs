using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MaxHealEffect : LevelUpEffect
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
                playerController.health.maxHealth = playerController.health.maxHealth + 75;
                playerController.health.currentHealth = playerController.health.currentHealth + 25;
                //scale up the player.transform.scale by 1.1f
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
