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

    public GameObject Minion;
    public Transform minionPosition;
    public MeleeMinion meleeMinion;
    public float maxHealth;

    public void SyncValues(GameObject minion, Transform position)
    {
        Minion = minion;
        minionPosition = position;
        if (minion != null)
        {
            meleeMinion = minion.GetComponent<MeleeMinion>();
            if (meleeMinion != null)
            {
                maxHealth = meleeMinion.startingHealth;
                InitializeHealthBar();
                meleeMinion.Health.OnValueChanged += UpdateHealthBar;
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
        if(Minion == null)
        {
            Destroy(healthBar);
        } 
        if(minionPosition != null)
        {
            healthBar.transform.position = new Vector3(minionPosition.position.x, minionPosition.position.y + 1.5f, minionPosition.position.z);
        }
    }
}