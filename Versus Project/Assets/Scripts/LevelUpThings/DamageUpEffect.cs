using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageUpEffect : LevelUpEffect
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
                Player.WeaponParent.weapons[0].damage = Player.WeaponParent.weapons[0].damage + 4;
                Player.WeaponParent.weapons[1].damage = Player.WeaponParent.weapons[1].damage + 8;
                Player.WeaponParent.weapons[2].GetComponent<Staff>().projectileDamage = Player.WeaponParent.weapons[2].GetComponent<Staff>().projectileDamage + 10;
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
