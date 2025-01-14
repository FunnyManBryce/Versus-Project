using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private BasePlayerController playerController;
    private float maxHealth;

    private void OnEnable()
    {
        var localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            playerController = localPlayer.GetComponent<BasePlayerController>();
            if (playerController != null)
            {
                maxHealth = playerController.maxHealth;
                InitializeHealthBar();
                playerController.currentHealth.OnValueChanged += UpdateHealthBar;
                UpdateHealthBar(0, playerController.currentHealth.Value);
            }
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.currentHealth.OnValueChanged -= UpdateHealthBar;
        }
    }

    private void InitializeHealthBar()
    {
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
}