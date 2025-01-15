using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private Health health;
    private float maxHealth;

    public bool initializedHealth;

    private void OnEnable()
    {
        var localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            health = localPlayer.GetComponent<Health>();
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
            maxHealth = health.maxHealth.Value;
            health.currentHealth.OnValueChanged += UpdateHealthBar;
            UpdateHealthBar(0, health.currentHealth.Value);
            Debug.Log("Max health is " + health.maxHealth.Value);
            Debug.Log("Current health is " + health.currentHealth.Value);
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        UpdateHealthText(maxHealth);
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
        if (initializedHealth == false && health.initialValuesSynced == true)
        {
            InitializeHealthBar();
            initializedHealth = true;
        }
    }
}