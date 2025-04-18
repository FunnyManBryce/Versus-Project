using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class GreedPlayerController : BasePlayerController
{
    // Passive - Gold conversion
    public float goldConversionRatio = 0.1f; // Base amount of damage gained per gold
    public NetworkVariable<float> goldDamageBonus = new NetworkVariable<float>();
    public GameObject Greed;

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
    public float lifeStealDuration = 5f;
    public float lifeStealRatio = 0.3f;
    public GameObject slamAOEPrefab;
    private Dictionary<NetworkObject, float> lifeStealTargets = new Dictionary<NetworkObject, float>();

    // Ultimate - Uncivilized Rage
    public RuntimeAnimatorController UltAnimator;
    public RuntimeAnimatorController NormalAnimator;
    public float ultimateDuration = 8f;
    public float ultMovementSpeedIncrease = 1.5f;
    public float ultPassiveMultiplier = 2.0f;
    public float missingHealthHealRatio = 0.05f; // Heal for 5% of missing health per second during ult
    private bool isUltActive = false;

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

        // Set Greed as melee character
        isMelee = true;
    }


    // Quick Punch (Ability 1) Implementation
    public void QuickPunchHostCheck()
    {
        if (!IsOwner) return;
        QuickPunchServerRpc();
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
        UncivRageServerRpc();
    }
    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;

        // Update passive gold conversion
        float goldBonus = Gold.Value * goldConversionRatio;
        if (isUltActive) goldBonus *= ultPassiveMultiplier;
        UpdateGoldDamageBonusServerRpc(goldBonus);

        // Ability locks
        if (animator.GetBool("AbilityOne") == true)
        {
            UncivRage.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == true)
        {
            UncivRage.preventAbilityUse = true;
            QuickPunch.preventAbilityUse = true;
        }
        if (animator.GetBool("Ult") == true)
        {
            QuickPunch.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
        }
        if (animator.GetBool("AutoAttack") == true)
        {
            UncivRage.preventAbilityUse = true;
            QuickPunch.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
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

    [ServerRpc]
    private void UpdateGoldDamageBonusServerRpc(float bonus)
    {
        goldDamageBonus.Value = bonus;
        // Apply to base damage for real-time updates
        float baseDamageWithoutBonus = BaseDamage.Value - goldDamageBonus.Value;
        BaseDamage.Value = baseDamageWithoutBonus + bonus;
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
    public void QuickPunchServerRpc()
    {
        bAM.PlayServerRpc("Greed Punch", Greed.transform.position);
        bAM.PlayClientRpc("Greed Punch", Greed.transform.position);

        // Calculate dash direction based on facing
        Vector2 dashDirection = PlayerSprite.flipX ? Vector2.left : Vector2.right;
        StartCoroutine(DashCoroutine(dashDirection));

        // Damage in a cone in front of the player
        Vector2 origin = Greed.transform.position;
        Vector2 forward = dashDirection;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(origin, punchRange);

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                // Check if target is within cone angle
                Vector2 directionToTarget = ((Vector2)collider.transform.position - origin).normalized;
                float angle = Vector2.Angle(forward, directionToTarget);

                if (angle <= punchConeAngle / 2)
                {
                    float damage = attackDamage * punchDamageMultiplier;
                    collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(NetworkObject), armorPen, false);

                    // Apply life steal if target is marked
                    if (lifeStealTargets.ContainsKey(collider.GetComponent<NetworkObject>()))
                    {
                        float healAmount = damage * lifeStealRatio;
                        health.currentHealth.Value = Mathf.Min(health.currentHealth.Value + healAmount, health.maxHealth.Value);
                    }
                }
            }
        }
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        isDashing = true;
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(direction.x, direction.y, 0) * dashDistance;

        while (Time.time < startTime + (dashDistance / dashSpeed))
        {
            float t = (Time.time - startTime) / (dashDistance / dashSpeed);
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

        // Instantiate slam AOE effect
        var slam = Instantiate(slamAOEPrefab, Greed.transform.position, Quaternion.identity);
        var slamNetworkObject = slam.GetComponent<NetworkObject>();
        slamNetworkObject.SpawnWithOwnership(clientID);

        // Apply damage and stun in radius
        Vector2 origin = Greed.transform.position;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(origin, slamRadius);

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                // Apply damage
                float damage = attackDamage * slamDamageMultiplier;
                collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(NetworkObject), armorPen, false);

                // Apply stun
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Immobilized", 1, stunDuration, true);

                // Mark for life steal
                NetworkObject targetObj = collider.GetComponent<NetworkObject>();
                lifeStealTargets[targetObj] = Time.time + lifeStealDuration;

                // Visual effect for marking
                health.InflictBuffServerRpc(targetObj, "Marked", 1, lifeStealDuration, true);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void UncivRageServerRpc()
    {
        Debug.Log("Greed Ultimate is happening!");
        isUltActive = true;
        animator.runtimeAnimatorController = UltAnimator;
        UltAnimChangeClientRpc();

        TriggerBuffServerRpc("Speed", ultMovementSpeedIncrease, ultimateDuration, true);

        IEnumerator coroutine = UltimateDuration();
        StartCoroutine(coroutine);
    }

    public IEnumerator UltimateDuration()
    {
        yield return new WaitForSeconds(ultimateDuration);
        yield return new WaitUntil(() => (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false));

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
            QuickPunch.cooldown -= 1f;
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
            lifeStealRatio += 0.1f;
        }
        if (GroundSlam.abilityLevel == 4)
        {
            slamRadius += 1f;
        }
        if (GroundSlam.abilityLevel == 5)
        {
            lifeStealDuration += 2f;
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