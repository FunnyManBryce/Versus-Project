using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    public Health health;
    private float maxHealth;
    private bool initialized = false;
    //public bool initializedHealth;

    private void OnEnable()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged += UpdateHealthBar;
            maxHealth = health.maxHealth.Value;
            UpdateHealthBar(0, health.currentHealth.Value);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged -= UpdateHealthBar;
        }
    }

    public void InitializeHealthBar()
    {
        if (health != null && !initialized)
        {
            maxHealth = health.maxHealth.Value;
            health.currentHealth.OnValueChanged += UpdateHealthBar;
            UpdateHealthBar(0, health.currentHealth.Value);
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health.currentHealth.Value;
            UpdateHealthText(health.currentHealth.Value);
            Debug.Log("Max health is " + health.maxHealth.Value);
            Debug.Log("Current health is " + health.currentHealth.Value);
            initialized = true;
        }

        UpdateHealthText(maxHealth);
    }

    private void UpdateHealthBar(float previousHealth, float newHealth)
    {
        healthSlider.value = newHealth;
        UpdateHealthText(newHealth);
    }

    private void UpdateHealthText(float currentHealth)
    {
        healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
    }
}