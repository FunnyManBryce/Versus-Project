using TMPro;
using UnityEngine;

[System.Serializable]
public class AbilityBase<T> where T : BasePlayerController
{
    public System.Action activateAbility;
    public System.Action abilityLevelUp;

    //public TextMeshProUGUI cooldownTextDisplay;
    //public Image cooldownImageDisplay;
    [Space]
    public T playerController;
    [Space]
    public KeyCode inputKey;
    public float cooldown;
    public float manaCost;
    public float lastUsed;
    public int abilityLevel;
    public bool manualUse;
    public bool OffCooldown() => lastUsed + (cooldown * ((100 - playerController.cDR) / 100)) < Time.time;
    public float NormalizedCooldown() => Mathf.Min((Time.time - lastUsed) / (cooldown * ((100 - playerController.cDR) / 100)), 1);
    public string CooldownDurationLeft() => ((cooldown * ((100 - playerController.cDR)/100)) - (Time.time - lastUsed)).ToString("0.00");
    public bool CanUse() => OffCooldown() && playerController.mana >= manaCost;
    public virtual void OnUse()
    {
        playerController.mana -= manaCost;
        lastUsed = Time.time;
    }
    public void AttemptUse()
    {
        if (!Input.GetKeyDown(inputKey) || !CanUse()) return;
        activateAbility();
        if (manualUse) return;
        OnUse();
    }

    /*public void UpdateCooldownDisplay()
    {
        cooldownTextDisplay.text = OffCooldown() ? "" : CooldownDurationLeft();
        cooldownImageDisplay.fillAmount = 1 - NormalizedCooldown();
    }*/
}

