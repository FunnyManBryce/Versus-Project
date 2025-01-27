using TMPro;
using UnityEngine;

[System.Serializable]
public class AbilityBase<T> where T : BasePlayerController
{
    public System.Action activateAbility;

    //public TextMeshProUGUI cooldownTextDisplay;
    //public Image cooldownImageDisplay;
    [Space]
    public T playerController;
    [Space]
    public KeyCode inputKey;
    public float cooldown;
    public float manaCost;
    public float lastUsed;
    public bool OffCooldown() => lastUsed + cooldown < Time.time;
    public float NormalizedCooldown() => Mathf.Min((Time.time - lastUsed) / cooldown, 1);
    public string CooldownDurationLeft() => (cooldown - (Time.time - lastUsed)).ToString("0.00");
    public bool CanUse() => OffCooldown() && playerController.mana >= manaCost;
    public virtual void OnUse()
    {
        playerController.mana -= manaCost;
        lastUsed = Time.time;
    }
    public void AttemptUse()
    {
        if (!Input.GetKeyDown(inputKey) || !CanUse()) return;
        OnUse();
        activateAbility();
    }

    /*public void UpdateCooldownDisplay()
    {
        cooldownTextDisplay.text = OffCooldown() ? "" : CooldownDurationLeft();
        cooldownImageDisplay.fillAmount = 1 - NormalizedCooldown();
    }*/
}

