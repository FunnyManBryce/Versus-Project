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
    public bool isUnlocked;
    public float cooldown;
    public float manaCost;
    public float lastUsed;
    public float pointClickTimeUsed;
    public int abilityLevel;
    public bool abilityBeingUsed;
    public bool waitingForClick = false;
    public bool preventAbilityUse;
    public bool OffCooldown() => lastUsed + (cooldown * ((100 - playerController.cDR) / 100)) < Time.time;
    public float NormalizedCooldown() => Mathf.Min((Time.time - lastUsed) / (cooldown * ((100 - playerController.cDR) / 100)), 1);
    public string CooldownDurationLeft() => ((cooldown * ((100 - playerController.cDR) / 100)) - (Time.time - lastUsed)).ToString("0.00");
    public bool CanUse() => OffCooldown() && playerController.mana >= manaCost;
    public bool PointAndClickDelay() => pointClickTimeUsed + 0.5f > Time.time;
    public virtual void OnUse()
    {
        playerController.mana -= manaCost;
        lastUsed = Time.time;
    }
    public void AttemptUse()
    {
        if (!Input.GetKeyDown(inputKey) || !CanUse() || preventAbilityUse || !isUnlocked) return;
        activateAbility();
        OnUse();
    }
    public void PointAndClickUse()
    {
        if (!Input.GetKeyDown(inputKey) || !CanUse() || PointAndClickDelay()) return;
        pointClickTimeUsed = Time.time;
        waitingForClick = !waitingForClick;
    }


    /*public void UpdateCooldownDisplay()
    {
        cooldownTextDisplay.text = OffCooldown() ? "" : CooldownDurationLeft();
        cooldownImageDisplay.fillAmount = 1 - NormalizedCooldown();
    }*/
}

