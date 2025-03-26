using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldownPrefab : MonoBehaviour
{
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI abilityNameText;

    public Slider CooldownSlider => cooldownSlider;
    public Image FillImage => fillImage;
    public TextMeshProUGUI CooldownText => cooldownText;
    public TextMeshProUGUI AbilityNameText => abilityNameText;

    public void SetAbilityName(string name)
    {
        if (abilityNameText != null)
        {
            abilityNameText.text = name;
        }
    }

    public void SetCooldownTime(float remainingTime)
    {
        if (cooldownText != null)
        {
            cooldownText.text = remainingTime > 0 ? remainingTime.ToString("F1") + "s" : "Ready";
        }
    }

    public void SetSliderValue(float value, float maxValue)
    {
        if (cooldownSlider != null)
        {
            cooldownSlider.maxValue = maxValue;
            cooldownSlider.value = value;
        }
    }

    public void SetFillColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
}