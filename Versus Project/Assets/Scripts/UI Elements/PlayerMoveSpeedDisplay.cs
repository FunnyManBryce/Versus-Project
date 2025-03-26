using TMPro;
using UnityEngine;

public class PlayerMoveSpeedDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveSpeedText;
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
            UpdateMoveSpeedText();
        }
    }
    private void UpdateMoveSpeedText()
    {
        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"MS: {playerController.maxSpeed:F0}";
        }
    }
}
