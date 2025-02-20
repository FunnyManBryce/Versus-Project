using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerArmorDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI armorText;
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
            UpdateArmorText();
        }
    }
    private void UpdateArmorText()
    {
        if (armorText != null)
        {
            armorText.text = $"Armor: {playerController.armor:F2}";
        }
    }
}
