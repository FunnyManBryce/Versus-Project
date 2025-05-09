using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI respawnText;
    [SerializeField] private BasePlayerController playerController;
    public bool initialized;
    [SerializeField] private bool isPlayer1UI;
    [SerializeField] private bool isPlayerDead;
    [SerializeField] private GameObject deathScreen;

    private void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        FindAndSetPlayerController();
    }
    private void FindAndSetPlayerController()
    {
        var players = Object.FindObjectsOfType<BasePlayerController>();

        foreach (var player in players)
        {
            if (player.NetworkObject.IsSpawned)
            {
                bool isPlayer1 = player.NetworkObject.OwnerClientId == 0;

                if (isPlayer1 == isPlayer1UI)
                {
                    playerController = player;
                    if (playerController != null)
                    {
                        initialized = true;
                        break;
                    }
                }
            }
        }
    }
    private void Update()
    {
        if (playerController == null || !initialized)
        {
            FindAndSetPlayerController();
        }
        else
        {
            isPlayerDead = playerController.isDead.Value;
            if(isPlayerDead)
            {
                deathScreen.SetActive(true);
            } else
            {
                deathScreen.SetActive(false);
            }
            RespawnText();
        }
    }
    private void RespawnText()
    {
        if (respawnText != null)
        {
            respawnText.text = "erm";
        }
    }
}
