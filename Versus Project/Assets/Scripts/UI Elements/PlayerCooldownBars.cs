using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting;

public class PlayerCooldownBars : MonoBehaviour
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private BasePlayerController playerController;
    [SerializeField] private GameObject abilityIcon;
    
    //Ability Description text
    [SerializeField] private GameObject Description;
    [SerializeField] private TextMeshProUGUI descCooldown;
    [SerializeField] private TextMeshProUGUI descName;
    [SerializeField] private TextMeshProUGUI descManaCost;
    [SerializeField] private TextMeshProUGUI descDescription;
    [SerializeField] private TextMeshProUGUI descLevelUpEffect;

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
                descDescription.text = "Description: " + player.AOE.abilityDescription;
                break;
            case 2: // Second ability (Shockwave)
                isOffCooldown = () => player.Shockwave.OffCooldown();
                getNormalizedCooldown = () => player.Shockwave.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Shockwave.CooldownDurationLeft();
                getManaCost = () => player.Shockwave.manaCost;
                cooldownDuration = player.Shockwave.cooldown;
                descDescription.text = "Description: " + player.Shockwave.abilityDescription;
                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                descDescription.text = "Description: " + player.Ultimate.abilityDescription;
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
                descDescription.text = "Description: " + player.String.abilityDescription;
                break;
            case 2: // Second ability (ModeSwitch)
                isOffCooldown = () => player.ModeSwitch.OffCooldown();
                getNormalizedCooldown = () => player.ModeSwitch.NormalizedCooldown();
                getCooldownTimeLeft = () => player.ModeSwitch.CooldownDurationLeft();
                getManaCost = () => player.ModeSwitch.manaCost;
                cooldownDuration = player.ModeSwitch.cooldown;
                descDescription.text = "Description: " + player.ModeSwitch.abilityDescription;
                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                descDescription.text = "Description: " + player.Ultimate.abilityDescription;
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
        if (playerController is DecayPlayerController decayPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    descManaCost.text = "Mana Cost: " + decayPlayer.AOE.manaCost;
                    descCooldown.text = "Cooldown: " + (decayPlayer.AOE.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + decayPlayer.AOE.abilityDescription;
                    descName.text =  decayPlayer.AOE.abilityName;
                    if (decayPlayer.AOE.isUnlocked && decayPlayer.AOE.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + decayPlayer.AOE.levelUpEffects[decayPlayer.AOE.abilityLevel];
                    }
                    else if (!decayPlayer.AOE.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.AOE.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 2: // Second ability (Shockwave)
                    descManaCost.text = "Mana Cost: " + decayPlayer.Shockwave.manaCost;
                    descCooldown.text = "Cooldown: " + (decayPlayer.Shockwave.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + decayPlayer.Shockwave.abilityDescription;
                    descName.text = decayPlayer.Shockwave.abilityName;
                    if (decayPlayer.Shockwave.isUnlocked && decayPlayer.Shockwave.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + decayPlayer.Shockwave.levelUpEffects[decayPlayer.Shockwave.abilityLevel];
                    }
                    else if (!decayPlayer.Shockwave.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.Shockwave.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 3: // Ultimate
                    descManaCost.text = "Mana Cost: " + decayPlayer.Ultimate.manaCost;
                    descCooldown.text = "Cooldown: " + (decayPlayer.Ultimate.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + decayPlayer.Ultimate.abilityDescription;
                    descName.text = decayPlayer.Ultimate.abilityName;
                    if (decayPlayer.Ultimate.isUnlocked && decayPlayer.Ultimate.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + decayPlayer.Ultimate.levelUpEffects[decayPlayer.Ultimate.abilityLevel];
                    }
                    else if (!decayPlayer.Ultimate.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.Ultimate.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
            }
        }
        else if (playerController is PuppeteeringPlayerController puppetPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (String)
                    descManaCost.text = "Mana Cost: " + puppetPlayer.String.manaCost;
                    descCooldown.text = "Cooldown: " + (puppetPlayer.String.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + puppetPlayer.String.abilityDescription;
                    descName.text = puppetPlayer.String.abilityName;
                    if (puppetPlayer.String.isUnlocked && puppetPlayer.String.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.String.levelUpEffects[puppetPlayer.String.abilityLevel];
                    }
                    else if (!puppetPlayer.String.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (puppetPlayer.String.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 2: // Second ability (ModeSwitch)
                    descManaCost.text = "Mana Cost: " + puppetPlayer.ModeSwitch.manaCost;
                    descCooldown.text = "Cooldown: " + (puppetPlayer.ModeSwitch.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + puppetPlayer.ModeSwitch.abilityDescription;
                    descName.text = puppetPlayer.ModeSwitch.abilityName;
                    if (puppetPlayer.ModeSwitch.isUnlocked && puppetPlayer.ModeSwitch.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.ModeSwitch.levelUpEffects[puppetPlayer.ModeSwitch.abilityLevel];
                    }
                    else if (!puppetPlayer.ModeSwitch.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (puppetPlayer.ModeSwitch.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 3: // Ultimate
                    descManaCost.text = "Mana Cost: " + puppetPlayer.Ultimate.manaCost;
                    descCooldown.text = "Cooldown: " + (puppetPlayer.Ultimate.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + puppetPlayer.Ultimate.abilityDescription;
                    descName.text = puppetPlayer.Ultimate.abilityName;
                    if (puppetPlayer.Ultimate.isUnlocked && puppetPlayer.Ultimate.abilityLevel < 5)
                    {
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.Ultimate.levelUpEffects[puppetPlayer.Ultimate.abilityLevel];
                    }
                    else if(!puppetPlayer.Ultimate.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    } else if(puppetPlayer.Ultimate.abilityLevel == 5)
                    {
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
            }
        }
        /*else if (playerController is GreedPlayerController greedPlayer)
        {

        }*/
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
        if (playerController is DecayPlayerController decayPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    decayPlayer.AOE.abilityLevelUp();

                    break;
                case 2: // Second ability (Shockwave)
                    decayPlayer.Shockwave.abilityLevelUp();

                    break;
                case 3: // Ultimate
                    decayPlayer.Ultimate.abilityLevelUp();
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
        else if (playerController is GreedPlayerController greedPlayer)
        {

        }
    }

    public void MouseEnter()
    {
        Description.SetActive(true);
    }
    public void MouseExit()
    {
        Description.SetActive(false);
    }

}