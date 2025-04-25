using TMPro;
using UnityEngine;

public class PlayerUnspentUpgradsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI upgradesText;
    [SerializeField] private TextMeshProUGUI unlocksText;
    [SerializeField] private GameObject unspentUnlocks;


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
            UpdateUpgradesText();
        }
    }
    private void UpdateUpgradesText()
    {
        if (upgradesText != null)
        {
            upgradesText.text = $"Unspent Upgrades: {playerController.unspentUpgrades.Value}";
        }
        if (unlocksText != null)
        {
            unlocksText.text = $"Unspent Unlocks: {playerController.unspentUnlocks.Value}";
            if(playerController.unspentUnlocks.Value == 0)
            {
                unspentUnlocks.SetActive(false);
            } else
            {
                unspentUnlocks.SetActive(true);
            }
        }
    }
}
