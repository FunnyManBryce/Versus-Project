using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    //[SerializeField] private TextMeshProUGUI healthText;

    public GameObject Object;
    public Transform objPosition;
    public Health health;
    public float maxHealth;
    public float offset = 1.5f;

    public bool initializedHealth;

    public void Start()
    {
        healthBar.transform.position = new Vector3(10000, 10000, 10000);
    }

    public void SyncValues(GameObject gameObject, Transform position, float offsetFromObject)
    {
        Object = gameObject;
        objPosition = position;
        offset = offsetFromObject;
        health = Object.GetComponent<Health>();
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
        if (Object != null)
        {
            health = Object.GetComponent<Health>();
            if (health != null)
            {
                maxHealth = health.maxHealth.Value;
                health.maxHealth.OnValueChanged += UpdateMaxHealth;
                health.currentHealth.OnValueChanged += UpdateHealthBar;
            }
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health.currentHealth.Value;
        }

        //UpdateHealthText(maxHealth);
    }

    private void UpdateMaxHealth(float previousMaxHealth, float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = newMaxHealth;
        }
        UpdateHealthBar(0, health.currentHealth.Value);
    }

    private void UpdateHealthBar(float previousHealth, float newHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
        }

        float healthPercentage = newHealth / maxHealth;
    }

    void Update()
    {
        if (initializedHealth == false && health.initialValuesSynced == true)
        {
            InitializeHealthBar();
            initializedHealth = true;
        }
        if (Object == null)
        {
            Destroy(healthBar);
        }
        if (objPosition != null)
        {
            healthBar.transform.position = new Vector3(objPosition.position.x, objPosition.position.y + offset, objPosition.position.z);
        }
    }
}