using TMPro;
using UnityEngine;

public class PlayerGoldDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI GoldText;
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
            UpdateGoldText(playerController.Gold.Value);
        }
    }
    private void UpdateGoldText(int Gold)
    {
        if (GoldText != null)
        {
            GoldText.text = $"Gold: {Gold:F0}";
        }
    }
}
