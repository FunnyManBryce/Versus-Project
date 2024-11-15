using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackEffect : LevelUpEffect
{
    public player Player;
    public playerController PlayerController;

    public override void ApplyEffect()
    {
        GameObject player = GameObject.FindWithTag("Player");


        if (player != null)
        {
            //refrence the player not player controller
            Player = player.GetComponent<player>();
            PlayerController = player.GetComponent<playerController>();

            if (Player != null)
            {
                Player.WeaponParent.weapons[0].knockbackMultiplier = Player.WeaponParent.weapons[0].knockbackMultiplier + 0.3f;
                Player.WeaponParent.weapons[1].knockbackMultiplier = Player.WeaponParent.weapons[1].knockbackMultiplier + 0.6f;
                PlayerController.pauseMenu.SetActive(true);
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
