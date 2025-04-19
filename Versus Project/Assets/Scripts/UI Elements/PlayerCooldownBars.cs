using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class PlayerCooldownBars : MonoBehaviour
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private BasePlayerController playerController;
    [SerializeField] private GameObject abilityIcon;

    public bool initializedCooldown;
    [SerializeField] private bool isPlayer1UI;
    [SerializeField] private int abilityIndex; // 0 for passive, 1 for ability1, 2 for ability2, 3 for ultimate

    // Instead of direct reference, use delegates to access ability properties
    private Func<bool> isOffCooldown;
    private Func<float> getNormalizedCooldown;
    private Func<string> getCooldownTimeLeft;
    private Func<float> getManaCost;
    private float cooldownDuration;

    private Color normalColor = new Color(0.706f, 0.851f, 0.702f, 1);
    private Color insufficientManaColor = new Color(0.722f, 0.427f, 0.427f, 1);

    private void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        FindAndSetPlayerController();
    }

    private void FindAndSetPlayerController()
    {
        var players = UnityEngine.Object.FindObjectsOfType<BasePlayerController>();
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
                        InitializeCooldownBar();
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (playerController == null || !initializedCooldown)
        {
            FindAndSetPlayerController();
        }
        else
        {
            UpdateCooldownBar();
        }
    }

    private void OnEnable()
    {
        FindAndSetPlayerController();
    }

    private void InitializeCooldownBar()
    {
        if (playerController != null)
        {
            // Handle different player controller types
            if (playerController is DecayPlayerController decayPlayer)
            {
                SetupAbilityDelegates(decayPlayer);
            }
            else if (playerController is PuppeteeringPlayerController puppetPlayer)
            {
                SetupAbilityDelegates(puppetPlayer);
            }
            else if (playerController is GreedPlayerController greedPlayer)
            {
                SetupAbilityDelegates(greedPlayer);
            }
            // Add other player controller types here as needed

            if (isOffCooldown != null)
            {
                if (cooldownSlider != null)
                {
                    cooldownSlider.maxValue = 1f;
                    cooldownSlider.value = isOffCooldown() ? 1f : getNormalizedCooldown();
                }

                UpdateCooldownText();
                UpdateManaCostText();
                initializedCooldown = true;
            }
        }
    }

    private void SetupAbilityDelegates(DecayPlayerController player)
    {
        switch (abilityIndex)
        {
            case 1: // First ability (AOE)
                isOffCooldown = () => player.AOE.OffCooldown();
                getNormalizedCooldown = () => player.AOE.NormalizedCooldown();
                getCooldownTimeLeft = () => player.AOE.CooldownDurationLeft();
                getManaCost = () => player.AOE.manaCost;
                cooldownDuration = player.AOE.cooldown;
                break;
            case 2: // Second ability (Shockwave)
                isOffCooldown = () => player.Shockwave.OffCooldown();
                getNormalizedCooldown = () => player.Shockwave.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Shockwave.CooldownDurationLeft();
                getManaCost = () => player.Shockwave.manaCost;
                cooldownDuration = player.Shockwave.cooldown;

                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                break;
        }
    }

    private void SetupAbilityDelegates(PuppeteeringPlayerController player)
    {
        switch (abilityIndex)
        {
            case 1: // First ability (String)
                isOffCooldown = () => player.String.OffCooldown();
                getNormalizedCooldown = () => player.String.NormalizedCooldown();
                getCooldownTimeLeft = () => player.String.CooldownDurationLeft();
                getManaCost = () => player.String.manaCost;
                cooldownDuration = player.String.cooldown;
                break;
            case 2: // Second ability (ModeSwitch)
                isOffCooldown = () => player.ModeSwitch.OffCooldown();
                getNormalizedCooldown = () => player.ModeSwitch.NormalizedCooldown();
                getCooldownTimeLeft = () => player.ModeSwitch.CooldownDurationLeft();
                getManaCost = () => player.ModeSwitch.manaCost;
                cooldownDuration = player.ModeSwitch.cooldown;
                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                break;
        }
    }

    private void SetupAbilityDelegates(GreedPlayerController player)
    {
        switch (abilityIndex)
        {
            case 0: // Passive - doesn't have cooldown but including for completeness
                isOffCooldown = () => true;
                getNormalizedCooldown = () => 1f;
                getCooldownTimeLeft = () => "0.00";
                getManaCost = () => 0f;
                cooldownDuration = 0f;
                break;
            case 1: // First ability (QuickPunch)
                isOffCooldown = () => player.QuickPunch.OffCooldown();
                getNormalizedCooldown = () => player.QuickPunch.NormalizedCooldown();
                getCooldownTimeLeft = () => player.QuickPunch.CooldownDurationLeft();
                getManaCost = () => player.QuickPunch.manaCost;
                cooldownDuration = player.QuickPunch.cooldown;
                break;
            case 2: // Second ability (GroundSlam)
                isOffCooldown = () => player.GroundSlam.OffCooldown();
                getNormalizedCooldown = () => player.GroundSlam.NormalizedCooldown();
                getCooldownTimeLeft = () => player.GroundSlam.CooldownDurationLeft();
                getManaCost = () => player.GroundSlam.manaCost;
                cooldownDuration = player.GroundSlam.cooldown;
                break;
            case 3: // Ultimate (UncivRage)
                isOffCooldown = () => player.UncivRage.OffCooldown();
                getNormalizedCooldown = () => player.UncivRage.NormalizedCooldown();
                getCooldownTimeLeft = () => player.UncivRage.CooldownDurationLeft();
                getManaCost = () => player.UncivRage.manaCost;
                cooldownDuration = player.UncivRage.cooldown;
                break;
        }
    }

    private void UpdateCooldownBar()
    {
        if (isOffCooldown == null) return;

        if (cooldownSlider != null)
        {
            // Update slider to show remaining cooldown (inverted - full when ready)
            cooldownSlider.value = isOffCooldown() ? 1f : getNormalizedCooldown();

            // Change fill color based on if ability can be used (mana check)
            if (fillImage != null)
            {
                fillImage.color = isOffCooldown() ?
                    (playerController.mana >= getManaCost() ? normalColor : insufficientManaColor) :
                    Color.gray;
            }
        }

        UpdateCooldownText();
        UpdateManaCostText();
    }

    private void UpdateCooldownText()
    {
        if (cooldownText != null)
        {
            if (isOffCooldown())
            {
                cooldownText.text = playerController.mana >= getManaCost() ? "READY" : "NO MANA";
            }
            else
            {
                cooldownText.text = getCooldownTimeLeft();
            }
        }
    }

    private void UpdateManaCostText()
    {
        if (manaCostText != null)
        {
            // Display the mana cost as text
            manaCostText.text = getManaCost().ToString("0");

            // Color the text based on whether the player has enough mana
            manaCostText.color = playerController.mana >= getManaCost() ?
                Color.white :
                insufficientManaColor;
        }
    }

    public void AbilityUpgrade()
    {
        Debug.Log("ermwhatballs");
        if (playerController is DecayPlayerController decayPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    decayPlayer.AOE.abilityLevelUp();
                    Debug.Log("1");

                    break;
                case 2: // Second ability (Shockwave)
                    decayPlayer.Shockwave.abilityLevelUp();
                    Debug.Log("2");

                    break;
                case 3: // Ultimate
                    decayPlayer.Ultimate.abilityLevelUp();
                    Debug.Log("3");
                    break;
            }
        }
        else if (playerController is PuppeteeringPlayerController puppetPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    puppetPlayer.String.abilityLevelUp();
                    break;
                case 2: // Second ability (Shockwave)
                    puppetPlayer.ModeSwitch.abilityLevelUp();

                    break;
                case 3: // Ultimate
                    puppetPlayer.Ultimate.abilityLevelUp();
                    break;
            }
        }
        /*else if (playerController is GreedPlayerController greedPlayer)
        {

        }*/
    }

}