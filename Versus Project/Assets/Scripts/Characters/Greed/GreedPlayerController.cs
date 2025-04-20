using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class GreedPlayerController : BasePlayerController
{
    // Passive - Gold conversion
    public float goldConversionRatio = 0.1f; // Base amount of damage gained per gold
    public GameObject Greed;
    public float basePlayerDamage;

    // Ability 1 - Quick Punch with Dash
    public float dashDistance = 3f;
    public float dashSpeed = 20f;
    public float punchDamageMultiplier = 0.8f;
    public float punchConeAngle = 60f;
    public float punchRange = 2.5f;
    private bool isDashing = false;

    // Ability 2 - Ground Slam
    public float slamDamageMultiplier = 0.5f;
    public float slamRadius = 3f;
    public float stunDuration = 0.5f;

    // Ultimate - Uncivilized Rage
    public RuntimeAnimatorController UltAnimator;
    public RuntimeAnimatorController NormalAnimator;
    public float ultimateDuration = 8f;
    public float ultMovementSpeedIncrease = 1.5f;
    public float ultPassiveMultiplier = 2.0f;
    public float missingHealthHealRatio = 0.05f; // Heal for 5% of missing health per second during ult
    public float ultDashDistance = 5f; // New: Distance to dash to target during ultimate cast
    public float ultDashSpeed = 30f; // New: Speed of dash during ultimate cast
    private bool isUltActive = false;
    private Dictionary<NetworkObject, float> lifeStealTargets = new Dictionary<NetworkObject, float>();

    // Ability references
    public AbilityBase<GreedPlayerController> QuickPunch;
    public AbilityBase<GreedPlayerController> GroundSlam;
    public AbilityBase<GreedPlayerController> UncivRage;


    new private void Awake()
    {
        base.Awake();
        QuickPunch.activateAbility = AbilityOneAnimation;
        GroundSlam.activateAbility = AbilityTwoAnimation;
        UncivRage.activateAbility = UltimateAnimation;
        QuickPunch.abilityLevelUp = QuickPunchLevelUp;
        GroundSlam.abilityLevelUp = GroundSlamLevelUp;
        UncivRage.abilityLevelUp = UltLevelUp;

        isMelee = true;
    }


    // Quick Punch (Ability 1) Implementation
    public void QuickPunchHostCheck()
    {
        if (!IsOwner) return;

        // Get mouse position in world space for dash direction
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPosition = Greed.transform.position;
        Vector2 dashDirection = (mousePosition - playerPosition).normalized;

        QuickPunchServerRpc(dashDirection);
    }

    // Ground Slam (Ability 2) Implementation
    public void GroundSlamHostCheck()
    {
        if (!IsOwner) return;
        GroundSlamServerRpc();
    }


    // Ultimate Implementation
    public void UncivRageHostCheck()
    {
        if (!IsOwner) return;

        // Get mouse position for dash target during ultimate
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPosition = Greed.transform.position;
        Vector2 dashDirection = (mousePosition - playerPosition).normalized;

        UncivRageServerRpc(dashDirection);
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;

        UpdateGoldPassive();

        if (animator.GetBool("AbilityOne") == true)
        {
            GroundSlam.preventAbilityUse = true;
            UncivRage.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == true)
        {
            QuickPunch.preventAbilityUse = true;
            UncivRage.preventAbilityUse = true;
        }
        if (animator.GetBool("Ult") == true)
        {
            QuickPunch.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
        }
        if (animator.GetBool("AutoAttack") == true)
        {
            QuickPunch.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
            UncivRage.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false &&
            animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
        {
            UncivRage.preventAbilityUse = false;
            QuickPunch.preventAbilityUse = false;
            GroundSlam.preventAbilityUse = false;
        }

        // Attempt abilities
        UncivRage.AttemptUse();
        QuickPunch.AttemptUse();
        GroundSlam.AttemptUse();

        // Apply ult healing based on missing health
        if (isUltActive)
        {
            float missingHealth = health.maxHealth.Value - health.currentHealth.Value;
            float healAmount = missingHealth * missingHealthHealRatio * Time.deltaTime;
            if (healAmount > 0)
            {
                HealFromUltServerRpc(healAmount);
            }
        }

        // Process life steal from marked targets
        List<NetworkObject> removeTargets = new List<NetworkObject>();
        foreach (var target in lifeStealTargets)
        {
            if (Time.time >= target.Value)
            {
                removeTargets.Add(target.Key);
            }
        }
        foreach (var target in removeTargets)
        {
            lifeStealTargets.Remove(target);
        }
    }

    private void UpdateGoldPassive()
    {
        float goldMultiplier = isUltActive ? ultPassiveMultiplier : 1.0f;
        float goldBonus = Gold.Value * goldConversionRatio * goldMultiplier;

        // Update the attack damage with base damage plus gold bonus
        if (IsOwner)
        {
            UpdateAttackDamageServerRpc(basePlayerDamage + goldBonus);
        }
    }

    [ServerRpc]
    private void UpdateAttackDamageServerRpc(float newAttackDamage)
    {
        attackDamage = newAttackDamage;
        SyncAttackDamageClientRpc(newAttackDamage);
    }

    [ClientRpc]
    private void SyncAttackDamageClientRpc(float newAttackDamage)
    {
        attackDamage = newAttackDamage;
    }

    [ServerRpc]
    private void HealFromUltServerRpc(float amount)
    {
        if (isUltActive)
        {
            health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + amount, health.maxHealth.Value);
        }
    }


    [Rpc(SendTo.Server)]
    public void QuickPunchServerRpc(Vector2 dashDirection)
    {
        bAM.PlayServerRpc("Greed Punch", Greed.transform.position);
        bAM.PlayClientRpc("Greed Punch", Greed.transform.position);

        // Damage in a cone in front of the player
        Vector2 origin = Greed.transform.position;
        Vector2 forward = dashDirection;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(origin, punchRange);

        bool hitPlayerController = false;

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                Vector2 directionToTarget = ((Vector2)collider.transform.position - origin).normalized;
                float angle = Vector2.Angle(forward, directionToTarget);

                if (angle <= punchConeAngle / 2)
                {
                    float damage = attackDamage * punchDamageMultiplier;
                    collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(NetworkObject), armorPen, false);

                    // Check if we hit a player controller
                    if (collider.GetComponent<BasePlayerController>() != null)
                    {
                        hitPlayerController = true;
                    }
                }
            }
        }

        // If we hit a player controller, reduce the cooldown by 50%
        if (hitPlayerController)
        {
            QuickPunch.lastUsed += QuickPunch.cooldown * 0.5f;
        }

        // Use the provided dash direction from mouse position
        StartCoroutine(DashCoroutine(dashDirection, dashDistance, dashSpeed));

        // Update sprite direction to match dash direction
        if (dashDirection.x < 0)
        {
            SetSpriteDirectionServerRpc(true); // flip sprite to face left
        }
        else if (dashDirection.x > 0)
        {
            SetSpriteDirectionServerRpc(false); // sprite faces right
        }
    }

    [ServerRpc]
    private void SetSpriteDirectionServerRpc(bool flipX)
    {
        SetSpriteDirectionClientRpc(flipX);
    }

    [ClientRpc]
    private void SetSpriteDirectionClientRpc(bool flipX)
    {
        PlayerSprite.flipX = flipX;
    }

    // Modified to accept distance and speed parameters for reuse with ultimate
    private IEnumerator DashCoroutine(Vector2 direction, float distance, float speed)
    {
        isDashing = true;
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(direction.x, direction.y, 0) * distance;

        while (Time.time < startTime + (distance / speed))
        {
            float t = (Time.time - startTime) / (distance / speed);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        isDashing = false;
    }


    [Rpc(SendTo.Server)]
    public void GroundSlamServerRpc()
    {
        bAM.PlayServerRpc("Greed Slam", Greed.transform.position);
        bAM.PlayClientRpc("Greed Slam", Greed.transform.position);

        // Apply damage and stun in radius (direct detection)
        Vector2 origin = Greed.transform.position;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(origin, slamRadius);

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                // Apply damage
                float damage = attackDamage * slamDamageMultiplier;
                collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(NetworkObject), armorPen, false);

                // Apply custom stun that reduces stats to zero
                NetworkObject targetObj = collider.GetComponent<NetworkObject>();
                if (targetObj.TryGetComponent<BasePlayerController>(out var targetController))
                {
                    targetController.TriggerBuffServerRpc("Speed", -targetController.BaseSpeed.Value, stunDuration, true);
                    targetController.TriggerBuffServerRpc("Auto Attack Speed", -targetController.BaseAttackSpeed.Value, stunDuration, true);
                    targetController.TriggerBuffServerRpc("Attack Damage", -targetController.BaseDamage.Value, stunDuration, true);
                    health.InflictBuffServerRpc(targetObj, "Immobilized", 1, stunDuration, true);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void UncivRageServerRpc(Vector2 dashDirection)
    {
        Debug.Log("Greed Ultimate is happening!");
        isUltActive = true;
        animator.runtimeAnimatorController = UltAnimator;
        UltAnimChangeClientRpc();

        TriggerBuffServerRpc("Speed", ultMovementSpeedIncrease, ultimateDuration, true);

        // Perform initial dash to target position
        StartCoroutine(DashCoroutine(dashDirection, ultDashDistance, ultDashSpeed));

        // Update sprite direction to match dash direction
        if (dashDirection.x < 0)
        {
            SetSpriteDirectionServerRpc(true); // flip sprite to face left
        }
        else if (dashDirection.x > 0)
        {
            SetSpriteDirectionServerRpc(false); // sprite faces right
        }

        // Start the ultimate duration timer
        IEnumerator coroutine = UltimateDuration();
        StartCoroutine(coroutine);
    }

    public IEnumerator UltimateDuration()
    {
        yield return new WaitForSeconds(ultimateDuration);
        // Wait until no animations are playing before ending the ultimate
        yield return new WaitUntil(() => (animator.GetBool("AbilityTwo") == false &&
                                         animator.GetBool("AbilityOne") == false &&
                                         animator.GetBool("Ult") == false &&
                                         animator.GetBool("AutoAttack") == false));

        isUltActive = false;
        animator.runtimeAnimatorController = NormalAnimator;
        UltEndAnimChangeClientRpc();
    }

    [ClientRpc]
    public void UltAnimChangeClientRpc()
    {
        animator.runtimeAnimatorController = UltAnimator;
    }

    [ClientRpc]
    public void UltEndAnimChangeClientRpc()
    {
        animator.runtimeAnimatorController = NormalAnimator;
    }

    // Ability Level Up Effects
    [ServerRpc(RequireOwnership = false)]
    public void SyncAbilityLevelServerRpc(int abilityNumber)
    {
        if (abilityNumber == 0)
        {
            PassiveLevelUp();
        }
        if (abilityNumber == 1)
        {
            QuickPunchLevelUp();
        }
        if (abilityNumber == 2)
        {
            GroundSlamLevelUp();
        }
        if (abilityNumber == 3)
        {
            UltLevelUp();
        }
    }

    public void PassiveLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
        }
        else
        {
            SyncAbilityLevelServerRpc(0);
        }

        // Improve gold conversion ratio
        goldConversionRatio += 0.05f;
    }

    //Level Up Effects
    #region
    public void QuickPunchLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            QuickPunch.abilityLevel++;
        }
        else
        {
            QuickPunch.abilityLevel++;
            SyncAbilityLevelServerRpc(1);
        }

        if (QuickPunch.abilityLevel == 2)
        {
            punchDamageMultiplier += 0.2f;
        }
        if (QuickPunch.abilityLevel == 3)
        {
            dashDistance += 1f;
        }
        if (QuickPunch.abilityLevel == 4)
        {
            punchConeAngle += 15f;
        }
        if (QuickPunch.abilityLevel == 5)
        {
            QuickPunch.cooldown -= 0.2f;
        }
    }

    public void GroundSlamLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            GroundSlam.abilityLevel++;
        }
        else
        {
            GroundSlam.abilityLevel++;
            SyncAbilityLevelServerRpc(2);
        }

        if (GroundSlam.abilityLevel == 2)
        {
            stunDuration += 0.3f;
        }
        if (GroundSlam.abilityLevel == 3)
        {
            GroundSlam.cooldown -= 2f;
        }
        if (GroundSlam.abilityLevel == 4)
        {
            slamRadius += 1f;
        }
        if (GroundSlam.abilityLevel == 5)
        {
            GroundSlam.cooldown -= 2f;
        }
    }

    public void UltLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            UncivRage.abilityLevel++;
        }
        else
        {
            UncivRage.abilityLevel++;
            SyncAbilityLevelServerRpc(3);
        }

        if (UncivRage.abilityLevel == 2)
        {
            missingHealthHealRatio += 0.02f;
        }
        if (UncivRage.abilityLevel == 3)
        {
            ultPassiveMultiplier += 0.5f;
        }
        if (UncivRage.abilityLevel == 4)
        {
            ultimateDuration += 3f;
        }
        if (UncivRage.abilityLevel == 5)
        {
            UncivRage.cooldown -= 20f;
        }
    }
    #endregion
}