using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    //[SerializeField] private TextMeshProUGUI healthText;

    public GameObject minion;
    public Transform minionPosition;
    public MeleeMinion meleeMinion;
    private float maxHealth;

    private void OnEnable()
    {
        // Find the local player's controller
        if (minion != null)
        {
            meleeMinion = minion.GetComponent<MeleeMinion>();
            if (meleeMinion != null)
            {
                maxHealth = meleeMinion.startingHealth;
                InitializeHealthBar();
                // Subscribe to health changes
                meleeMinion.Health.OnValueChanged += UpdateHealthBar;
                // Set initial health
                UpdateHealthBar(0, meleeMinion.Health.Value);
            }
        }
    }

    /*private void OnDisable()
    {
        if (meleeMinion != null)
        {
            meleeMinion.Health.OnValueChanged -= UpdateHealthBar;
        }
    }*/

    private void InitializeHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        //UpdateHealthText(maxHealth);
    }

    private void UpdateHealthBar(float previousHealth, float newHealth)
    {
        if (newHealth <= 0)
        {
            Destroy(healthBar);
        }
        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
        }

        float healthPercentage = newHealth / maxHealth;
        //UpdateHealthText(newHealth);
    }

    /*private void UpdateHealthText(float currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }*/

    void Update()
    {
        healthBar.transform.position = new Vector3(minionPosition.position.x, minionPosition.position.y + 1.5f, minionPosition.position.z);
    }
}