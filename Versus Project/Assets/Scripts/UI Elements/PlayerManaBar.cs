using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerManaBar : MonoBehaviour
{
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private BasePlayerController playerController;
    public bool initializedMana;
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
                        InitializeManaBar();
                        break;
                    }
                }
            }
        }
    }
    private void Update()
    {
        if (playerController == null || !initializedMana)
        {
            FindAndSetPlayerController();
        }
        else
        {
            UpdateManaBar(playerController.mana);
        }
    }
    private void OnEnable()
    {
        FindAndSetPlayerController();
    }

    private void InitializeManaBar()
    {
        if (playerController != null)
        {
            if (manaSlider != null)
            {
                manaSlider.maxValue = playerController.maxMana;
                manaSlider.value = playerController.mana;
            }

            UpdateManaText(playerController.mana);
            initializedMana = true;
        }
    }

    public void UpdateMaxMana(float newMaxMana)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = newMaxMana;
        }
        UpdateManaText(playerController.mana);
    }
    private void UpdateManaBar(float newMana)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = playerController.maxMana; 
            manaSlider.value = newMana;
        }
        UpdateManaText(newMana);
    }

    private void UpdateManaText(float currentMana)
    {
        if (manaText != null)
        {
            manaText.text = $"{Mathf.Ceil(currentMana)}/{playerController.maxMana}";
        }
    }
}