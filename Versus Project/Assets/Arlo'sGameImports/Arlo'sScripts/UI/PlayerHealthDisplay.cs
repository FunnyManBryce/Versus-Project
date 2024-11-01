using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHealthDisplay : MonoBehaviour
{
    public TMP_Text playerHealthText;
    public TMP_Text playerXPText;
    
    public Health playerHealth;
    public playerController player;

    private void Update()
    {
        if (playerHealth != null)
        {
            UpdateHealthText();
        }
    }
    void UpdateHealthText()
    {
        playerHealthText.text = "HP:" + playerHealth.currentHealth + "/" + playerHealth.maxHealth;
        playerXPText.text = "XP:" + player.currentExperience + "/" + player.maxExperience;
    }
}
