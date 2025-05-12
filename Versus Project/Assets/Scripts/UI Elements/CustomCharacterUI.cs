using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CustomCharacterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Text;
    [SerializeField] private BasePlayerController playerController;
    public bool initialized;
    [SerializeField] private bool isPlayer1UI;

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
            CustomText();
        }
    }
    private void CustomText()
    {
        if (Text != null)
        {
            if (playerController != null)
            {
                // Handle different player controller types
                if (playerController is DecayPlayerController decayPlayer)
                {
                    if(!decayPlayer.isUlting.Value)
                    {
                        Text.text = $"Stat Decay: {decayPlayer.totalStatDecay.Value}";
                    } else
                    {
                        Text.text = $"Stat Decay: 0";
                    }
                }
                else if (playerController is PuppeteeringPlayerController puppetPlayer)
                {
                    Text.text = $"Max Puppets: {puppetPlayer.maxPuppets.Value}";
                }
                else if (playerController is GreedPlayerController greedPlayer)
                {
                    Text.text = null;
                    //Could include how much damage greed is getting from gold
                }
                else if (playerController is VoidPlayerController voidPlayer)
                {
                    Text.text = null;
                }
            }
        }
    }
}
