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
    [SerializeField] private GameObject abilityLock;

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

    [SerializeField] private GameObject upgradeButton;
    [SerializeField] private bool isUnlocked;
    [SerializeField] private bool isMaxLevel;

    // Instead of direct reference, use delegates to access ability properties
    private Func<bool> isOffCooldown;
    private Func<float> getNormalizedCooldown;
    private Func<string> getCooldownTimeLeft;
    private Func<float> getManaCost;
    private float cooldownDuration;

    private Color normalColor = new Color(0.706f, 0.950f, 0.702f, 1);
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
        if(isUnlocked)
        {
            abilityLock.GetComponent<Animator>().SetTrigger("Unlocking");
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
            else if (playerController is VoidPlayerController voidPlayer)
            {
                SetupAbilityDelegates(voidPlayer);
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
                abilityIcon.GetComponent<Image>().sprite = player.AOE.abilityIcon;
                break;
            case 2: // Second ability (Shockwave)
                isOffCooldown = () => player.Shockwave.OffCooldown();
                getNormalizedCooldown = () => player.Shockwave.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Shockwave.CooldownDurationLeft();
                getManaCost = () => player.Shockwave.manaCost;
                cooldownDuration = player.Shockwave.cooldown;
                descDescription.text = "Description: " + player.Shockwave.abilityDescription;
                abilityIcon.GetComponent<Image>().sprite = player.Shockwave.abilityIcon;
                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                descDescription.text = "Description: " + player.Ultimate.abilityDescription;
                abilityIcon.GetComponent<Image>().sprite = player.Ultimate.abilityIcon;
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
                abilityIcon.GetComponent<Image>().sprite = player.String.abilityIcon;
                break;
            case 2: // Second ability (ModeSwitch)
                isOffCooldown = () => player.ModeSwitch.OffCooldown();
                getNormalizedCooldown = () => player.ModeSwitch.NormalizedCooldown();
                getCooldownTimeLeft = () => player.ModeSwitch.CooldownDurationLeft();
                getManaCost = () => player.ModeSwitch.manaCost;
                cooldownDuration = player.ModeSwitch.cooldown;
                descDescription.text = "Description: " + player.ModeSwitch.abilityDescription;
                abilityIcon.GetComponent<Image>().sprite = player.ModeSwitch.abilityIcon;
                break;
            case 3: // Ultimate
                isOffCooldown = () => player.Ultimate.OffCooldown();
                getNormalizedCooldown = () => player.Ultimate.NormalizedCooldown();
                getCooldownTimeLeft = () => player.Ultimate.CooldownDurationLeft();
                getManaCost = () => player.Ultimate.manaCost;
                cooldownDuration = player.Ultimate.cooldown;
                descDescription.text = "Description: " + player.Ultimate.abilityDescription;
                abilityIcon.GetComponent<Image>().sprite = player.Ultimate.abilityIcon;
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
                abilityIcon.GetComponent<Image>().sprite = player.QuickPunch.abilityIcon;
                cooldownText.color = Color.black;
                break;
            case 2: // Second ability (GroundSlam)
                isOffCooldown = () => player.GroundSlam.OffCooldown();
                getNormalizedCooldown = () => player.GroundSlam.NormalizedCooldown();
                getCooldownTimeLeft = () => player.GroundSlam.CooldownDurationLeft();
                getManaCost = () => player.GroundSlam.manaCost;
                cooldownDuration = player.GroundSlam.cooldown;
                abilityIcon.GetComponent<Image>().sprite = player.GroundSlam.abilityIcon;
                cooldownText.color = Color.black;
                break;
            case 3: // Ultimate (UncivRage)
                isOffCooldown = () => player.UncivRage.OffCooldown();
                getNormalizedCooldown = () => player.UncivRage.NormalizedCooldown();
                getCooldownTimeLeft = () => player.UncivRage.CooldownDurationLeft();
                getManaCost = () => player.UncivRage.manaCost;
                cooldownDuration = player.UncivRage.cooldown;
                abilityIcon.GetComponent<Image>().sprite = player.UncivRage.abilityIcon;
                break;
        }
    }

    private void SetupAbilityDelegates(VoidPlayerController player)
    {
        switch (abilityIndex)
        {
            case 0: // Passive - Damage stacking
                isOffCooldown = () => true; // Passive is always active
                getNormalizedCooldown = () => 1f;
                getCooldownTimeLeft = () => "0.00";
                getManaCost = () => 0f;
                cooldownDuration = 0f;
                descDescription.text = "Description: Consecutive hits with Void Ball increase damage output";
                descName.text = "Void Corruption";
                descCooldown.text = "Stacks reset after " + player.passiveDuration + " seconds without hitting";
                descManaCost.text = "Current Stacks: " + player.passiveStacks.Value;
                descLevelUpEffect.text = "Next Level: Increases damage bonus and duration";
                break;

            case 1: // First ability (Void Ball)
                isOffCooldown = () => player.VoidBall.OffCooldown();
                getNormalizedCooldown = () => player.VoidBall.NormalizedCooldown();
                getCooldownTimeLeft = () => player.VoidBall.CooldownDurationLeft();
                getManaCost = () => player.VoidBall.manaCost;
                cooldownDuration = player.VoidBall.cooldown;
                descDescription.text = "Description: " + player.VoidBall.abilityDescription;
                descName.text = player.VoidBall.abilityName;
                if (player.VoidBall.isUnlocked && player.VoidBall.abilityLevel < 5)
                {
                    descLevelUpEffect.text = "Next Level: " + player.VoidBall.levelUpEffects[player.VoidBall.abilityLevel];
                }
                else if (!player.VoidBall.isUnlocked)
                {
                    descLevelUpEffect.text = "Spend an unlock to get this ability";
                }
                else if (player.VoidBall.abilityLevel == 5)
                {
                    descLevelUpEffect.text = "Max Level";
                }
                abilityIcon.GetComponent<Image>().sprite = player.VoidBall.abilityIcon;
                break;

            case 2: // Second ability (Blink)
                isOffCooldown = () => player.BlinkAbility.OffCooldown();
                getNormalizedCooldown = () => player.BlinkAbility.NormalizedCooldown();
                getCooldownTimeLeft = () => player.BlinkAbility.CooldownDurationLeft();
                getManaCost = () => player.BlinkAbility.manaCost;
                cooldownDuration = player.BlinkAbility.cooldown;
                descDescription.text = "Description: " + player.BlinkAbility.abilityDescription;
                descName.text = player.BlinkAbility.abilityName;
                if (player.BlinkAbility.isUnlocked && player.BlinkAbility.abilityLevel < 5)
                {
                    descLevelUpEffect.text = "Next Level: " + player.BlinkAbility.levelUpEffects[player.BlinkAbility.abilityLevel];
                }
                else if (!player.BlinkAbility.isUnlocked)
                {
                    descLevelUpEffect.text = "Spend an unlock to get this ability";
                }
                else if (player.BlinkAbility.abilityLevel == 5)
                {
                    descLevelUpEffect.text = "Max Level";
                }
                abilityIcon.GetComponent<Image>().sprite = player.BlinkAbility.abilityIcon;
                break;

            case 3: // Ultimate (Void Perspective)
                isOffCooldown = () => player.VoidPerspective.OffCooldown();
                getNormalizedCooldown = () => player.VoidPerspective.NormalizedCooldown();
                getCooldownTimeLeft = () => player.VoidPerspective.CooldownDurationLeft();
                getManaCost = () => player.VoidPerspective.manaCost;
                cooldownDuration = player.VoidPerspective.cooldown;
                descDescription.text = "Description: " + player.VoidPerspective.abilityDescription;
                descName.text = player.VoidPerspective.abilityName;
                if (player.VoidPerspective.isUnlocked && player.VoidPerspective.abilityLevel < 5)
                {
                    descLevelUpEffect.text = "Next Level: " + player.VoidPerspective.levelUpEffects[player.VoidPerspective.abilityLevel];
                }
                else if (!player.VoidPerspective.isUnlocked)
                {
                    descLevelUpEffect.text = "Spend an unlock to get this ability";
                }
                else if (player.VoidPerspective.abilityLevel == 5)
                {
                    descLevelUpEffect.text = "Max Level";
                }
                abilityIcon.GetComponent<Image>().sprite = player.VoidPerspective.abilityIcon;
                break;
        }
    }

    private void UpdateCooldownBar()
    {
        if (isOffCooldown == null) return;

        if(playerController.unspentUnlocks.Value > 0 && !isUnlocked)
        {
            upgradeButton.SetActive(true);
        }
        else if (playerController.unspentUpgrades.Value > 0 && !isMaxLevel && isUnlocked)
        {
            upgradeButton.SetActive(true);
        } else 
        {
            upgradeButton.SetActive(false);
        }
        if (cooldownSlider != null)
        {
            // Update slider to show remaining cooldown (inverted - full when ready)
            cooldownSlider.value = isOffCooldown() ? 1f : getNormalizedCooldown();

            // Change fill color based on if ability can be used (mana check)
            if (fillImage != null)
            {
                fillImage.color = isOffCooldown() ?
                    (playerController.mana >= getManaCost() ? normalColor : insufficientManaColor) :
                    Color.white;
            }
        }

        UpdateCooldownText();
        UpdateManaCostText();
    }

    private void UpdateCooldownText()
    {
        Debug.Log("ahh");
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
                        isUnlocked = true;
                    }
                    else if (!decayPlayer.AOE.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.AOE.abilityLevel == 5)
                    {
                        isMaxLevel = true;
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
                        isUnlocked = true;
                    }
                    else if (!decayPlayer.Shockwave.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.Shockwave.abilityLevel == 5)
                    {
                        isMaxLevel = true;
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
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + decayPlayer.Ultimate.levelUpEffects[decayPlayer.Ultimate.abilityLevel];
                    }
                    else if (!decayPlayer.Ultimate.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (decayPlayer.Ultimate.abilityLevel == 5)
                    {
                        isMaxLevel = true;
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
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.String.levelUpEffects[puppetPlayer.String.abilityLevel];
                    }
                    else if (!puppetPlayer.String.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (puppetPlayer.String.abilityLevel == 5)
                    {
                        isMaxLevel = true;
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
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.ModeSwitch.levelUpEffects[puppetPlayer.ModeSwitch.abilityLevel];
                    }
                    else if (!puppetPlayer.ModeSwitch.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (puppetPlayer.ModeSwitch.abilityLevel == 5)
                    {
                        isMaxLevel = true;
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
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + puppetPlayer.Ultimate.levelUpEffects[puppetPlayer.Ultimate.abilityLevel];
                    }
                    else if(!puppetPlayer.Ultimate.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    } else if(puppetPlayer.Ultimate.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
            }
        }
        else if (playerController is GreedPlayerController greedPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (String)
                    descManaCost.text = "";
                    descCooldown.text = "Cooldown: " + (greedPlayer.QuickPunch.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + greedPlayer.QuickPunch.abilityDescription;
                    descName.text = greedPlayer.QuickPunch.abilityName;
                    if (greedPlayer.QuickPunch.isUnlocked && greedPlayer.QuickPunch.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + greedPlayer.QuickPunch.levelUpEffects[greedPlayer.QuickPunch.abilityLevel];
                    }
                    else if (!greedPlayer.QuickPunch.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (greedPlayer.QuickPunch.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 2: // Second ability (ModeSwitch)
                    descManaCost.text = "";
                    descCooldown.text = "Cooldown: " + (greedPlayer.GroundSlam.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + greedPlayer.GroundSlam.abilityDescription;
                    descName.text = greedPlayer.GroundSlam.abilityName;
                    if (greedPlayer.GroundSlam.isUnlocked && greedPlayer.GroundSlam.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + greedPlayer.GroundSlam.levelUpEffects[greedPlayer.GroundSlam.abilityLevel];
                    }
                    else if (!greedPlayer.GroundSlam.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (greedPlayer.GroundSlam.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 3: // Ultimate
                    descManaCost.text = "";
                    descCooldown.text = "Cooldown: " + (greedPlayer.UncivRage.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + greedPlayer.UncivRage.abilityDescription;
                    descName.text = greedPlayer.UncivRage.abilityName;
                    if (greedPlayer.UncivRage.isUnlocked && greedPlayer.UncivRage.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + greedPlayer.UncivRage.levelUpEffects[greedPlayer.UncivRage.abilityLevel];
                    }
                    else if (!greedPlayer.UncivRage.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (greedPlayer.UncivRage.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
            }
        }
        else if (playerController is VoidPlayerController voidPlayer)
        {
            switch (abilityIndex)
            {
                case 0: // Passive
                    descManaCost.text = "Current Stacks: " + voidPlayer.passiveStacks.Value;
                    descCooldown.text = "Stacks reset after " + voidPlayer.passiveDuration + " seconds without hitting";
                    descDescription.text = "Description: Consecutive hits with Void Ball increase damage output";
                    descName.text = "Void Corruption";
                    descLevelUpEffect.text = voidPlayer.unspentUpgrades.Value > 0 ?
                        "Next Level: Increases damage bonus and duration" : "Max Level";
                    break;
                case 1: // First ability (Void Ball)
                    descManaCost.text = "Mana Cost: " + voidPlayer.VoidBall.manaCost;
                    descCooldown.text = "Cooldown: " + (voidPlayer.VoidBall.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + voidPlayer.VoidBall.abilityDescription;
                    descName.text = voidPlayer.VoidBall.abilityName;
                    if (voidPlayer.VoidBall.isUnlocked && voidPlayer.VoidBall.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + voidPlayer.VoidBall.levelUpEffects[voidPlayer.VoidBall.abilityLevel];
                    }
                    else if (!voidPlayer.VoidBall.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (voidPlayer.VoidBall.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 2: // Second ability (Blink)
                    descManaCost.text = "Mana Cost: " + voidPlayer.BlinkAbility.manaCost;
                    descCooldown.text = "Cooldown: " + (voidPlayer.BlinkAbility.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + voidPlayer.BlinkAbility.abilityDescription;
                    descName.text = voidPlayer.BlinkAbility.abilityName;
                    if (voidPlayer.BlinkAbility.isUnlocked && voidPlayer.BlinkAbility.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + voidPlayer.BlinkAbility.levelUpEffects[voidPlayer.BlinkAbility.abilityLevel];
                    }
                    else if (!voidPlayer.BlinkAbility.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (voidPlayer.BlinkAbility.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
                case 3: // Ultimate (Void Perspective)
                    descManaCost.text = "Mana Cost: " + voidPlayer.VoidPerspective.manaCost;
                    descCooldown.text = "Cooldown: " + (voidPlayer.VoidPerspective.cooldown * ((100 - playerController.cDR) / 100)) + " seconds";
                    descDescription.text = "Description: " + voidPlayer.VoidPerspective.abilityDescription;
                    descName.text = voidPlayer.VoidPerspective.abilityName;
                    if (voidPlayer.VoidPerspective.isUnlocked && voidPlayer.VoidPerspective.abilityLevel < 5)
                    {
                        isUnlocked = true;
                        descLevelUpEffect.text = "Next Level: " + voidPlayer.VoidPerspective.levelUpEffects[voidPlayer.VoidPerspective.abilityLevel];
                    }
                    else if (!voidPlayer.VoidPerspective.isUnlocked)
                    {
                        descLevelUpEffect.text = "Spend an unlock to get this ability";
                    }
                    else if (voidPlayer.VoidPerspective.abilityLevel == 5)
                    {
                        isMaxLevel = true;
                        descLevelUpEffect.text = "Max Level";
                    }
                    break;
            }
        }
        if (cooldownText != null)
        {
            if (isOffCooldown())
            {
                cooldownText.text = "";
                //cooldownText.text = playerController.mana >= getManaCost() ? "READY" : "NO MANA";
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
                Color.cyan :
                insufficientManaColor;
        }
    }

    public void AbilityUpgrade()
    {
        //abilityLock.GetComponent<Animator>().SetTrigger("Unlocking");
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
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    greedPlayer.QuickPunch.abilityLevelUp();
                    break;
                case 2: // Second ability (Shockwave)
                    greedPlayer.GroundSlam.abilityLevelUp();

                    break;
                case 3: // Ultimate
                    greedPlayer.UncivRage.abilityLevelUp();
                    break;
            }
        }
        else if (playerController is VoidPlayerController voidPlayer)
        {
            switch (abilityIndex)
            {
                case 1: // First ability (AOE)
                    voidPlayer.VoidBall.abilityLevelUp();
                    break;
                case 2: // Second ability (Shockwave)
                    voidPlayer.BlinkAbility.abilityLevelUp();

                    break;
                case 3: // Ultimate
                    voidPlayer.VoidPerspective.abilityLevelUp();
                    break;
            }
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