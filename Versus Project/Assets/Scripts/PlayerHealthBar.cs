using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    private BasePlayerController playerController;
    private Camera mainCamera;
    private float maxHealth;

    private void Start()
    {
        mainCamera = Camera.main;
        playerController = GetComponentInParent<BasePlayerController>();

        if (playerController != null)
        {
            maxHealth = playerController.maxHealth;
            healthSlider.maxValue = maxHealth;
            UpdateHealth(playerController.currentHealth.Value);
            Debug.Log("why would I think that would work");

            // get health changes
            playerController.currentHealth.OnValueChanged += OnHealthChanged;
        }
    }

    private void Update()
    {
        if (playerController == null) return;
    }
    private void OnHealthChanged(float previousValue, float newValue)
    {
        UpdateHealth(newValue);
    }

    private void UpdateHealth(float currentHealth)
    {
        // Update slider value
        healthSlider.value = currentHealth;

        // Update text
        healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{maxHealth}";
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }
}