using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoostEffect : LevelUpEffect
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
                playerController.maxSpeed = playerController.maxSpeed + 3f;
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
