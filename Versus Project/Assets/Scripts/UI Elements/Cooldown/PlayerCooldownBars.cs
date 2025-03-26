using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCooldownBars : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BasePlayerController playerController;
    [SerializeField] private bool isPlayer1UI;
    public bool initialized;

    [Header("Ability Cooldown UI")]
    [SerializeField] private GameObject abilityCooldownPrefab;
    [SerializeField] private Transform cooldownContainer;

    [Header("Passive Timer UI (Optional)")]
    [SerializeField] private GameObject passiveTimerPrefab;
    [SerializeField] private Transform passiveTimerContainer;
    [SerializeField] private bool hasPassiveTimer;

    // Cached references to ability cooldown UI elements
    private List<AbilityCooldownUI> abilityCooldowns = new List<AbilityCooldownUI>();
    private PassiveTimerUI passiveTimer;

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
                        InitializeCooldownBars();
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
            return;
        }

        UpdateCooldownBars();

        if (hasPassiveTimer && passiveTimer != null)
        {
            UpdatePassiveTimer();
        }
    }

    private void OnEnable()
    {
        FindAndSetPlayerController();
    }

    private void InitializeCooldownBars()
    {
        if (playerController == null) return;

        // Clear existing cooldown UI if any
        foreach (Transform child in cooldownContainer)
        {
            Destroy(child.gameObject);
        }
        abilityCooldowns.Clear();

        // Find and create UI for all abilities
        var abilities = FindAbilities();
        foreach (var ability in abilities)
        {
            GameObject cooldownUI = Instantiate(abilityCooldownPrefab, cooldownContainer);
            AbilityCooldownUI abilityUI = cooldownUI.GetComponent<AbilityCooldownUI>();

            if (ability != null && abilityUI != null)
            {
                abilityUI.SetupAbility(ability);
                abilityCooldowns.Add(abilityUI);
            }
        }

        // Initialize passive timer if needed
        if (hasPassiveTimer)
        {
            InitializePassiveTimer();
        }

        initialized = true;
    }

    private System.Object[] FindAbilities()
    {
        List<System.Object> abilities = new List<System.Object>();

        var fields = playerController.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            // Check if field type is AbilityBase<> (any generic version)
            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(AbilityBase<>))
            {
                var ability = field.GetValue(playerController);
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }
        }

        return abilities.ToArray();
    }

    private void InitializePassiveTimer()
    {
        if (passiveTimerContainer != null)
        {
            foreach (Transform child in passiveTimerContainer)
            {
                Destroy(child.gameObject);
            }

            GameObject timerUI = Instantiate(passiveTimerPrefab, passiveTimerContainer);
            passiveTimer = timerUI.GetComponent<PassiveTimerUI>();

            // For Decay we know the passive is based on lastDecayTime
            if (playerController is DecayPlayerController)
            {
                DecayPlayerController decayController = (DecayPlayerController)playerController;
                passiveTimer.Setup("Passive", decayController.timeToDecay);
            }
        }
    }

    private void UpdateCooldownBars()
    {
        foreach (var cooldownUI in abilityCooldowns)
        {
            cooldownUI.UpdateUI();
        }
    }

    private void UpdatePassiveTimer()
    {
        if (playerController is DecayPlayerController)
        {
            DecayPlayerController decayController = (DecayPlayerController)playerController;
            float currentTime = decayController.lameManager.matchTimer.Value;
            float timeElapsed = currentTime - decayController.lastDecayTime;
            float remainingTime = Mathf.Max(0, decayController.timeToDecay - timeElapsed);

            passiveTimer.UpdateTimer(remainingTime, decayController.timeToDecay);
        }
    }
}

// Class to manage a single ability cooldown UI
[System.Serializable]
public class AbilityCooldownUI : MonoBehaviour
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI abilityNameText;

    private System.Object abilityObject;
    private System.Reflection.PropertyInfo cooldownProperty;
    private System.Reflection.PropertyInfo lastUsedProperty;
    private System.Reflection.PropertyInfo abilityLevelProperty;

    public void SetupAbility(System.Object ability)
    {
        abilityObject = ability;

        // Use reflection to get the properties we need regardless of the generic type
        System.Type abilityType = ability.GetType();
        cooldownProperty = abilityType.GetProperty("cooldown");
        lastUsedProperty = abilityType.GetProperty("lastUsed");
        abilityLevelProperty = abilityType.GetProperty("abilityLevel");

        // Get ability name (use the field name in the parent class)
        string abilityName = "Unknown";
        var fields = abilityType.DeclaringType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == abilityType && field.GetValue(abilityType.DeclaringType) == ability)
            {
                abilityName = field.Name;
                break;
            }
        }

        if (abilityNameText != null)
        {
            abilityNameText.text = abilityName;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (abilityObject == null || cooldownProperty == null || lastUsedProperty == null)
            return;

        float cooldown = (float)cooldownProperty.GetValue(abilityObject);
        float lastUsed = (float)lastUsedProperty.GetValue(abilityObject);
        int abilityLevel = (int)abilityLevelProperty.GetValue(abilityObject);

        float currentTime = Time.time;
        float timeElapsed = currentTime - lastUsed;
        float remainingTime = Mathf.Max(0, cooldown - timeElapsed);

        // Only show if ability is level 1 or mroe
        if (abilityLevel <= 0)
        {
            cooldownSlider.gameObject.SetActive(false);
            if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            return;
        }
        else
        {
            cooldownSlider.gameObject.SetActive(true);
            if (cooldownText != null) cooldownText.gameObject.SetActive(true);
        }

        // Update slider
        cooldownSlider.maxValue = cooldown;
        cooldownSlider.value = remainingTime > 0 ? remainingTime : 0;

        // Update text
        if (cooldownText != null)
        {
            if (remainingTime > 0)
            {
                cooldownText.text = remainingTime.ToString("F1") + "s";
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.text = "Ready";
                cooldownText.gameObject.SetActive(true);
            }
        }

        // Change fill color based on ready status
        if (fillImage != null)
        {
            fillImage.color = remainingTime > 0 ? Color.gray : Color.white;
        }
    }
}

[System.Serializable]
public class PassiveTimerUI : MonoBehaviour
{
    [SerializeField] private Slider timerSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI passiveNameText;

    public void Setup(string passiveName, float maxDuration)
    {
        if (passiveNameText != null)
        {
            passiveNameText.text = passiveName;
        }

        if (timerSlider != null)
        {
            timerSlider.maxValue = maxDuration;
        }
    }

    public void UpdateTimer(float remainingTime, float maxDuration)
    {
        // Update slider
        if (timerSlider != null)
        {
            timerSlider.maxValue = maxDuration;
            timerSlider.value = maxDuration - remainingTime;
        }

        // Update text
        if (timerText != null)
        {
            timerText.text = remainingTime.ToString("F1") + "s";
        }

        // Change fill color based on progress
        if (fillImage != null)
        {
            float progress = remainingTime / maxDuration;
            fillImage.color = Color.Lerp(Color.yellow, Color.red, 1 - progress);
        }
    }
}