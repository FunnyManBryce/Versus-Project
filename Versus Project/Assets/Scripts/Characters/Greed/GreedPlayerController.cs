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
    private float previousGoldValue = 0;

    // Ability 1 - Quick Punch with Dash
    public float dashDistance = 3f;
    public float dashSpeed = 20f;
    public float punchDamageMultiplier = 0.8f;
    public float punchConeAngle = 60f;
    public float punchRange = 2.5f;
    public float punchConeOffsetDistance = 0.75f;
    public float punchPlayerHitCooldownReduction = 0.5f;
    private bool isDashing = false;

    // Ability 2 - Ground Slam
    public float slamDamageMultiplier = 0.5f;
    public float slamRadius = 3f;
    public float stunDuration = 0.5f;

    // Ultimate - Uncivilized Rage
    public float ultimateDuration = 8f;
    public float ultMovementSpeedIncrease = 1.5f;
    public float ultPassiveMultiplier = 2.0f;
    public float missingHealthHealRatio = 0.02f; // Heal for 5% of missing health per second during ult
    private bool isUltActive = false;
    private Dictionary<NetworkObject, float> lifeStealTargets = new Dictionary<NetworkObject, float>();
    public float ultDashSpeed = 30f; // New variable for ult dash speed

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

    public void QuickPunchHostCheck()
    {
        if (!IsOwner) return;

        // Get mouse position in world space for dash direction
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPosition = Greed.transform.position;
        Vector2 dashDirection = (mousePosition - playerPosition).normalized;

        QuickPunchServerRpc(dashDirection);
    }
    public void GroundSlamHostCheck()
    {
        if (!IsOwner) return;
        GroundSlamServerRpc();
    }

    public void UncivRageHostCheck()
    {
        if (!IsOwner) return;

        // Get mouse position to find target for ultimate dash
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Find the nearest valid target near the mouse position
        NetworkObject targetObject = FindTargetNearPosition(mousePosition);

        if (targetObject != null)
        {
            UncivRageServerRpc(targetObject);
        }
        else
        {
            UncivRageServerRpc(new NetworkObjectReference());
        }
    }

    private NetworkObject FindTargetNearPosition(Vector2 position)
    {
        // Find nearby targets
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, 5f);

        NetworkObject closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null &&
                CanAttackTarget(collider.GetComponent<NetworkObject>()) &&
                collider.isTrigger)
            {
                float distance = Vector2.Distance(position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = collider.GetComponent<NetworkObject>();
                }
            }
        }

        return closestTarget;
    }

    new private void Update()
    {
        if (isStunned.Value)
        {
            isAttacking = false;
            animator.SetBool("AutoAttack", false);
        }

        base.Update();
        if (!IsOwner || isDead.Value) return;

        UpdateGoldPassive();

        // Ability locks - kept some basic locks to prevent ability spamming during animations
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
        if (animator.GetBool("AutoAttack") == true)
        {
            QuickPunch.preventAbilityUse = true;
            GroundSlam.preventAbilityUse = true;
            UncivRage.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
        {
            UncivRage.preventAbilityUse = false;
            QuickPunch.preventAbilityUse = false;
            GroundSlam.preventAbilityUse = false;
        }

        // Attempt abilities
        if (!isStunned.Value)
        {
            UncivRage.AttemptUse();
            QuickPunch.AttemptUse();
            GroundSlam.AttemptUse();
        }

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
    }

    private void UpdateGoldPassive()
    {
        // Calculate how gold affects damage
        float goldMultiplier = isUltActive ? ultPassiveMultiplier : 1.0f;
        float goldBonus = Gold.Value * goldConversionRatio * goldMultiplier;

        // Calculate total damage including base damage from the BaseDamage property plus gold bonus
        // BaseDamage already includes item bonuses from the BasePlayerController class
        float totalDamage = BaseDamage.Value + goldBonus;

        // Only update when owner to prevent multiple updates
        if (IsOwner)
        {
            UpdateAttackDamageServerRpc(totalDamage);
        }

        // Update previous gold value
        previousGoldValue = Gold.Value;
    }

    [ServerRpc(RequireOwnership = false)]
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

    [ServerRpc(RequireOwnership = false)]
    public void QuickPunchServerRpc(Vector2 dashDirection)
    {
        /*bAM.PlayServerRpc("Greed Punch", Greed.transform.position);
        bAM.PlayClientRpc("Greed Punch", Greed.transform.position);*/

        // Calculate an offset point behind the player based on dash direction
        Vector2 offsetDirection = -dashDirection; // Opposite of dash direction
        Vector2 playerPosition = Greed.transform.position;
        Vector2 offsetOrigin = playerPosition + (offsetDirection * punchConeOffsetDistance);

        // Now use this offset origin for the cone detection
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(offsetOrigin, punchRange);

        bool hitPlayerController = false;

        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                Vector2 directionToTarget = ((Vector2)collider.transform.position - offsetOrigin).normalized;
                float angle = Vector2.Angle(dashDirection, directionToTarget);

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

        if (hitPlayerController)
        {
            QuickPunch.lastUsed += QuickPunch.cooldown * (punchPlayerHitCooldownReduction);
        }

        // Start the dash coroutine on the server and sync to clients
        StartDashClientRpc(dashDirection, dashDistance, dashSpeed);

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

    [ClientRpc]
    private void StartDashClientRpc(Vector2 direction, float distance, float speed)
    {
        StartCoroutine(DashCoroutine(direction, distance, speed));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetSpriteDirectionServerRpc(bool flipX)
    {
        SetSpriteDirectionClientRpc(flipX);
    }

    [ClientRpc]
    private void SetSpriteDirectionClientRpc(bool flipX)
    {
        PlayerSprite.flipX = flipX;
    }

    private IEnumerator DashCoroutine(Vector2 direction, float distance, float speed)
    {
        isDashing = true;
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(direction.x, direction.y, 0) * distance;

        while (Time.time < startTime + (distance / speed))
        {
            float t = (Time.time - startTime) / (distance / speed);
            if (IsServer || IsOwner)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, t);
            }
            yield return null;
        }

        // Ensure we reach the final position
        if (IsServer || IsOwner)
        {
            transform.position = targetPos;
        }

        isDashing = false;
    }

    [ServerRpc(RequireOwnership = false)]
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
                health.InflictBuffServerRpc(targetObj, "Stun", 1, stunDuration, true);
            }
        }

        // Notify all clients that the slam happened
        GroundSlamEffectClientRpc(origin, slamRadius);
    }

    [ClientRpc]
    private void GroundSlamEffectClientRpc(Vector2 origin, float radius)
    {

    }

    [ServerRpc(RequireOwnership = false)]
    public void UncivRageServerRpc(NetworkObjectReference targetRef)
    {
        Debug.Log("Greed Ultimate is happening!");
        bAM.PlayServerRpc("Greed Ultimate", Greed.transform.position);
        bAM.PlayClientRpc("Greed Ultimate", Greed.transform.position);

        isUltActive = true;
        UltActiveClientRpc(true);

        // Check if we have a target to dash to
        if (targetRef.TryGet(out NetworkObject targetObject) && targetObject != null)
        {
            // Calculate direction and perform dash to target
            Vector2 dashDirection = ((Vector2)targetObject.transform.position - (Vector2)transform.position).normalized;
            float dashDistance = Vector2.Distance(transform.position, targetObject.transform.position);

            // Update sprite direction based on dash
            if (dashDirection.x < 0)
            {
                SetSpriteDirectionServerRpc(true); // flip sprite to face left
            }
            else if (dashDirection.x > 0)
            {
                SetSpriteDirectionServerRpc(false); // sprite faces right
            }

            // Start the ult dash on all clients
            UltDashClientRpc(dashDirection, dashDistance, ultDashSpeed);
        }

        TriggerBuffServerRpc("Speed", ultMovementSpeedIncrease, ultimateDuration, true);

        StartCoroutine(UltimateDuration());
    }

    [ClientRpc]
    private void UltActiveClientRpc(bool active)
    {
        isUltActive = active;
        if (active)
        {

            StartCoroutine(UltimateDuration());
        }
    }

    [ClientRpc]
    private void UltDashClientRpc(Vector2 direction, float distance, float speed)
    {
        StartCoroutine(DashCoroutine(direction, distance, speed));
    }

    public IEnumerator UltimateDuration()
    {
        yield return new WaitForSeconds(ultimateDuration);
        yield return new WaitUntil(() => (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false));

        if (IsServer)
        {
            isUltActive = false;
            UltActiveClientRpc(false);
        }
        else if (IsOwner)
        {
            // Client is ending the ultimate locally
            isUltActive = false;
        }
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
        if (!QuickPunch.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            QuickPunch.isUnlocked = true;
            if (IsServer)
            {
                unspentUnlocks.Value--;
            }
            else
            {
                SyncAbilityLevelServerRpc(1);
            }
        }
        else
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
                QuickPunch.cooldown -= 0.5f;
            }
            if (QuickPunch.abilityLevel == 3)
            {
                dashDistance += 1.5f;
                punchRange += 1.5f;
            }
            if (QuickPunch.abilityLevel == 4)
            {
                punchPlayerHitCooldownReduction += 0.25f;
            }
            if (QuickPunch.abilityLevel == 5)
            {
                punchDamageMultiplier += 0.2f;
            }
        }
    }

    public void GroundSlamLevelUp()
    {
        if (!GroundSlam.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            GroundSlam.isUnlocked = true;
            if (IsServer)
            {
                unspentUnlocks.Value--;
            }
            else
            {
                SyncAbilityLevelServerRpc(2);
            }
        }
        else
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
                stunDuration += 0.25f;
            }
            if (GroundSlam.abilityLevel == 3)
            {
                GroundSlam.cooldown -= 3f;
            }
            if (GroundSlam.abilityLevel == 4)
            {
                slamRadius += 1.5f;
            }
            if (GroundSlam.abilityLevel == 5)
            {
                slamDamageMultiplier += 0.4f;
            }
        }
    }

    public void UltLevelUp()
    {
        if (!UncivRage.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            UncivRage.isUnlocked = true;
            if (IsServer)
            {
                unspentUnlocks.Value--;
            }
            else
            {
                SyncAbilityLevelServerRpc(3);
            }
        }
        else
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
                UncivRage.cooldown -= 20f;
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
                missingHealthHealRatio += 0.05f;
            }
        }
    }
    #endregion
}