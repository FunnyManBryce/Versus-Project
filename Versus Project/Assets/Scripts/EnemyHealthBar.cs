using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public bool initializedHealth;

    public void Start()
    {
        healthBar.transform.position = new Vector3(10000, 10000, 10000);
    }

    public void SyncValues(GameObject gameObject, Transform position)
    {
        Object = gameObject;
        objPosition = position;
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
                health.currentHealth.OnValueChanged += UpdateHealthBar;
                UpdateHealthBar(0, health.currentHealth.Value);
            }
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        //UpdateHealthText(maxHealth);
    }

    private void UpdateHealthBar(float previousHealth, float newHealth)
    {
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
        if(initializedHealth == false && health.initialValuesSynced == true)
        {
            InitializeHealthBar();
            initializedHealth = true;
        }
        if(Object == null)
        {
            Destroy(healthBar);
        } 
        if(objPosition != null)
        {
            healthBar.transform.position = new Vector3(objPosition.position.x, objPosition.position.y + 1.5f, objPosition.position.z);
        }
    }
}