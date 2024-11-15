using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class invincibilityUpEffect : LevelUpEffect
{
    public playerController playerController;
    public PlayerBlinkFeedback playerIFrames;

    public override void ApplyEffect()
    {
        GameObject player = GameObject.FindWithTag("Player");


        if (player != null)
        {
            playerController = player.GetComponent<playerController>();
            playerIFrames = player.GetComponent<PlayerBlinkFeedback>();

            if (playerController != null)
            {
                playerController.health.invincibilityDuration = playerController.health.invincibilityDuration + 0.4f;
                playerIFrames.blinkCount = playerIFrames.blinkCount + 3;
                playerIFrames.blinkDuration = playerIFrames.blinkDuration + 0.4f;
                playerController.pauseMenu.SetActive(true);

                playerController.OnTakeDamage();
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
