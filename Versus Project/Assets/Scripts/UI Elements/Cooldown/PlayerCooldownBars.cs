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

    [Header("Debug Options")]
    [SerializeField] private bool enableDebugLogs = true;

    // Cached references to ability cooldown UI elements
    private List<AbilityCooldownUI> abilityCooldowns = new List<AbilityCooldownUI>();
    private PassiveTimerUI passiveTimer;

    // Track initialization attempts to avoid excessive logging
    private float lastInitAttemptTime = 0f;
    private const float INIT_RETRY_DELAY = 1.0f;

    private void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        LogDebug($"Starting PlayerCooldownBars for {(isPlayer1UI ? "Player 1" : "Player 2")} UI");

        // Initial attempt to find player controller
        FindAndSetPlayerController();
    }

    private void FindAndSetPlayerController()
    {
        if (Time.time - lastInitAttemptTime < INIT_RETRY_DELAY) return;
        lastInitAttemptTime = Time.time;

        LogDebug("Attempting to find and set player controller");

        var players = Object.FindObjectsOfType<BasePlayerController>();
        LogDebug($"Found {players.Length} BasePlayerController instances");

        foreach (var player in players)
        {
            if (player == null)
            {
                LogDebug("Found null player controller reference");
                continue;
            }

            if (player.NetworkObject == null)
            {
                LogDebug($"Player controller {player.name} has null NetworkObject");
                continue;
            }

            LogDebug($"Checking player: {player.name}, IsSpawned: {player.NetworkObject.IsSpawned}, OwnerClientId: {player.NetworkObject.OwnerClientId}");

            if (player.NetworkObject.IsSpawned)
            {
                bool isPlayer1 = player.NetworkObject.OwnerClientId == 0;
                LogDebug($"Player is spawned. IsPlayer1: {isPlayer1}, IsPlayer1UI: {isPlayer1UI}");

                if (isPlayer1 == isPlayer1UI)
                {
                    playerController = player;
                    LogDebug($"Found matching player controller: {player.name}");

                    if (playerController != null)
                    {
                        LogDebug("Initializing cooldown bars for this controller");
                        InitializeCooldownBars();
                        break;
                    }
                }
            }
        }

        if (playerController == null)
        {
            LogDebug("No matching player controller found. Will retry later.");
        }
    }

    private void Update()
    {
        if (playerController == null || !initialized)
        {
            // Retry finding player controller
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
        LogDebug("PlayerCooldownBars OnEnable");
        FindAndSetPlayerController();
    }

    private void InitializeCooldownBars()
    {
        if (playerController == null)
        {
            LogDebug("Cannot initialize cooldown bars: playerController is null");
            return;
        }

        LogDebug($"Initializing cooldown bars for {playerController.name}");

        // Clear existing cooldown UI if any
        foreach (Transform child in cooldownContainer)
        {
            Destroy(child.gameObject);
        }
        abilityCooldowns.Clear();

        // Find and create UI for all abilities
        var abilities = FindAbilities();
        LogDebug($"Found {abilities.Length} abilities");

        foreach (var ability in abilities)
        {
            LogDebug($"Creating UI for ability type: {ability.GetType().Name}");
            GameObject cooldownUI = Instantiate(abilityCooldownPrefab, cooldownContainer);
            AbilityCooldownUI abilityUI = cooldownUI.GetComponent<AbilityCooldownUI>();

            if (ability != null && abilityUI != null)
            {
                abilityUI.SetupAbility(ability);
                abilityCooldowns.Add(abilityUI);
                LogDebug("Added ability cooldown UI");
            }
            else
            {
                LogDebug($"Failed to set up ability UI: ability null? {ability == null}, abilityUI null? {abilityUI == null}");
            }
        }

        // Initialize passive timer if needed
        if (hasPassiveTimer)
        {
            LogDebug("Initializing passive timer");
            InitializePassiveTimer();
        }

        initialized = true;
        LogDebug("Cooldown bars initialization complete");
    }

    private System.Object[] FindAbilities()
    {
        List<System.Object> abilities = new List<System.Object>();

        LogDebug($"Searching for abilities in {playerController.GetType().Name}");

        var fields = playerController.GetType().GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        LogDebug($"Found {fields.Length} fields in player controller");

        foreach (var field in fields)
        {
            LogDebug($"Checking field: {field.Name}, Type: {field.FieldType.Name}");

            // Check if field type is AbilityBase<> (any generic version)
            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(AbilityBase<>))
            {
                LogDebug($"Field {field.Name} is an AbilityBase<> type");

                try
                {
                    var ability = field.GetValue(playerController);
                    if (ability != null)
                    {
                        LogDebug($"Adding ability from field {field.Name}");
                        abilities.Add(ability);
                    }
                    else
                    {
                        LogDebug($"Field {field.Name} contains null value");
                    }
                }
                catch (System.Exception ex)
                {
                    LogDebug($"Error getting ability value: {ex.Message}");
                }
            }
        }

        LogDebug($"Found {abilities.Count} abilities in total");
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
                LogDebug("Setting up passive timer for DecayPlayerController");
                DecayPlayerController decayController = (DecayPlayerController)playerController;
                passiveTimer.Setup("Decay", decayController.timeToDecay);
            }
            else
            {
                LogDebug($"Player controller is not DecayPlayerController, it's {playerController.GetType().Name}");
            }
        }
        else
        {
            LogDebug("passiveTimerContainer is null");
        }
    }

    private void UpdateCooldownBars()
    {
        foreach (var cooldownUI in abilityCooldowns)
        {
            if (cooldownUI != null)
            {
                cooldownUI.UpdateUI();
            }
        }
    }

    private void UpdatePassiveTimer()
    {
        if (playerController is DecayPlayerController)
        {
            DecayPlayerController decayController = (DecayPlayerController)playerController;
            float currentTime = decayController.lameManager?.matchTimer?.Value ?? 0f;
            float timeElapsed = currentTime - decayController.lastDecayTime;
            float remainingTime = Mathf.Max(0, decayController.timeToDecay - timeElapsed);

            passiveTimer.UpdateTimer(remainingTime, decayController.timeToDecay);
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CooldownBars] [{(isPlayer1UI ? "P1" : "P2")}] {message}");
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
    [SerializeField] private bool enableDebugLogs = false;

    private System.Object abilityObject;
    private System.Reflection.PropertyInfo cooldownProperty;
    private System.Reflection.PropertyInfo lastUsedProperty;
    private System.Reflection.PropertyInfo abilityLevelProperty;
    private string abilityName = "Unknown";

    public void SetupAbility(System.Object ability)
    {
        abilityObject = ability;

        if (ability == null)
        {
            LogDebug("SetupAbility: ability is null");
            return;
        }

        LogDebug($"Setting up ability UI for type: {ability.GetType().Name}");

        // Use reflection to get the properties we need regardless of the generic type
        System.Type abilityType = ability.GetType();
        cooldownProperty = abilityType.GetProperty("cooldown");
        lastUsedProperty = abilityType.GetProperty("lastUsed");
        abilityLevelProperty = abilityType.GetProperty("abilityLevel");

        LogDebug($"Found properties - cooldown: {cooldownProperty != null}, lastUsed: {lastUsedProperty != null}, abilityLevel: {abilityLevelProperty != null}");

        // Get ability name (use the field name in the parent class)
        abilityName = "Unknown";

        try
        {
            var declaringType = abilityType.DeclaringType;
            if (declaringType != null)
            {
                var fields = declaringType.GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType == abilityType)
                    {
                        // Get parent instance
                        var parent = ability.GetType().GetProperty("parent")?.GetValue(ability);
                        if (parent != null)
                        {
                            var fieldValue = field.GetValue(parent);
                            if (fieldValue == ability)
                            {
                                abilityName = field.Name;
                                LogDebug($"Found ability name: {abilityName}");
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            LogDebug($"Error finding ability name: {ex.Message}");
        }

        if (abilityNameText != null)
        {
            abilityNameText.text = abilityName;
            LogDebug($"Set ability name text to: {abilityName}");
        }
        else
        {
            LogDebug("abilityNameText is null");
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (abilityObject == null)
        {
            LogDebug("UpdateUI: abilityObject is null");
            return;
        }

        if (cooldownProperty == null || lastUsedProperty == null || abilityLevelProperty == null)
        {
            LogDebug("UpdateUI: One or more required properties are null");
            return;
        }

        try
        {
            float cooldown = (float)cooldownProperty.GetValue(abilityObject);
            float lastUsed = (float)lastUsedProperty.GetValue(abilityObject);
            int abilityLevel = (int)abilityLevelProperty.GetValue(abilityObject);

            float currentTime = Time.time;
            float timeElapsed = currentTime - lastUsed;
            float remainingTime = Mathf.Max(0, cooldown - timeElapsed);

            // Only show if ability is level 1 or more
            if (abilityLevel <= 0)
            {
                if (cooldownSlider != null) cooldownSlider.gameObject.SetActive(false);
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
                return;
            }
            else
            {
                if (cooldownSlider != null) cooldownSlider.gameObject.SetActive(true);
                if (cooldownText != null) cooldownText.gameObject.SetActive(true);
            }

            // Update slider
            if (cooldownSlider != null)
            {
                cooldownSlider.maxValue = cooldown;
                cooldownSlider.value = remainingTime > 0 ? remainingTime : 0;
            }

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
        catch (System.Exception ex)
        {
            LogDebug($"Error updating UI: {ex.Message}");
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AbilityUI] [{abilityName}] {message}");
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
    [SerializeField] private bool enableDebugLogs = false;

    private string passiveName = "Passive";

    public void Setup(string passiveName, float maxDuration)
    {
        this.passiveName = passiveName;
        LogDebug($"Setting up timer with name: {passiveName}, maxDuration: {maxDuration}");

        if (passiveNameText != null)
        {
            passiveNameText.text = passiveName;
        }
        else
        {
            LogDebug("passiveNameText is null");
        }

        if (timerSlider != null)
        {
            timerSlider.maxValue = maxDuration;
        }
        else
        {
            LogDebug("timerSlider is null");
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

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PassiveUI] [{passiveName}] {message}");
        }
    }
}