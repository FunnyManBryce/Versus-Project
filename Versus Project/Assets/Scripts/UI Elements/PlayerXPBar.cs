using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerXPBar : MonoBehaviour
{
    [SerializeField] private Slider xpSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private BasePlayerController playerController;
    public bool initializedXP;
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
                        InitializeXPBar();
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (playerController == null || !initializedXP)
        {
            FindAndSetPlayerController();
        }
        else
        {
            UpdateXPBar(playerController.XP.Value);
            UpdateLevelText(playerController.Level.Value);
        }
    }

    private void OnEnable()
    {
        FindAndSetPlayerController();
    }

    private void InitializeXPBar()
    {
        if (playerController != null)
        {
            if (xpSlider != null)
            {
                xpSlider.maxValue = playerController.XPToNextLevel.Value;
                xpSlider.value = playerController.XP.Value;
            }
            UpdateXPText(playerController.XP.Value);
            UpdateLevelText(playerController.Level.Value);
            initializedXP = true;
        }
    }

    public void UpdateMaxXP(float newMaxXP)
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = newMaxXP;
        }
        UpdateXPText(playerController.XP.Value);
    }

    private void UpdateXPBar(float newXP)
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = playerController.XPToNextLevel.Value;
            xpSlider.value = newXP;
        }
        UpdateXPText(newXP);
    }

    private void UpdateXPText(float currentXP)
    {
        if (xpText != null)
        {
            xpText.text = $"{Mathf.Floor(currentXP)}/{playerController.XPToNextLevel.Value}";
        }
    }

    private void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"LVL: {level}";
        }
    }
}