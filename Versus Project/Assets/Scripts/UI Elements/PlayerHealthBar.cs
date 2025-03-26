using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Health health;
    private float maxHealth;
    public bool initializedHealth;

    [SerializeField] private bool isPlayer1UI;


    private void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        FindAndSetPlayerHealth();
    }

    private void FindAndSetPlayerHealth()
    {
        var players = Object.FindObjectsOfType<BasePlayerController>();

        foreach (var player in players)
        {
            if (player.NetworkObject.IsSpawned)
            {
                bool isPlayer1 = player.NetworkObject.OwnerClientId == 0;

                // Only set the health reference if this UI matches the player number
                if (isPlayer1 == isPlayer1UI)
                {
                    health = player.GetComponent<Health>();
                    if (health != null)
                    {
                        InitializeHealthBar();
                        break;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var netObj = player.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                health = player.GetComponent<Health>();
                break;
            }
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged -= UpdateHealthBar;
        }
    }

    private void InitializeHealthBar()
    {
        if (health != null)
        {
            health.maxHealth.OnValueChanged += UpdateMaxHealth;
            health.currentHealth.OnValueChanged += UpdateHealthBar;
            maxHealth = health.maxHealth.Value;

            Debug.Log("Max health is " + health.maxHealth.Value);
            Debug.Log("Current health is " + health.currentHealth.Value);
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        UpdateHealthText(health.currentHealth.Value);
        initializedHealth = true;
    }
    private void UpdateMaxHealth(float previousMaxHealth, float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = newMaxHealth;
        }
        UpdateHealthText(health.currentHealth.Value);
    }

    private void UpdateHealthBar(float previousHealth, float newHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
        }

        float healthPercentage = newHealth / maxHealth;
        UpdateHealthText(newHealth);
    }

    private void UpdateHealthText(float currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }

    void Update()
    {
        if (health == null || !initializedHealth)
        {
            FindAndSetPlayerHealth();
        }
        if (initializedHealth == false && health.initialValuesSynced == true)
        {
            InitializeHealthBar();
            initializedHealth = true;
        }
    }
}