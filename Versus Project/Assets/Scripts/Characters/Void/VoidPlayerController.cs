using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VoidPlayerController : BasePlayerController
{
    // Core character references
    public GameObject VoidCore;
    public GameObject voidBallPrefab;

    // Multi-casting feature
    public int baseVoidBallCasts = 1;  // Default number of casts
    public float multiCastDelay = 0.5f; // Delay between multi-casts
    private int remainingCasts = 0;     // Track remaining casts in current sequence
    public NetworkVariable<int> currentMaxCasts = new NetworkVariable<int>();

    // Passive - Damage stacking
    public float passiveDamageIncrease = 0.1f;
    public float level5PassiveIncrease = 0.2f;
    public float baseUnaffectedDamage = 10f;
    public float passiveDuration = 8f; // Duration before stacks reset if no Q hits
    private float lastAbilityHitTime;
    public NetworkVariable<int> passiveStacks = new NetworkVariable<int>();
    public float baseAttackDamage;

    // Ability 1 - Void Ball
    public float dangerCircleRadius = 2.5f;
    public float dangerCircleWarningTime = 1.0f;
    public float ballReturnSpeed = 5f;
    public float ballDamageMultiplier = 0.7f;
    public float voidBallCastRange = 20f;
    public GameObject dangerCirclePrefab;

    // Ability 2 - Blink
    public float blinkDistance = 5f;

    // Ultimate - Void Perspective
    public float ultimateDuration = 8f;
    public float cameraZoomOutMultiplier = 2f;
    public float abilityCooldownReduction = 0.8f;
    public float movementSpeedReduction = 0.999f;
    public float voidBallUltimateRangeMultiplier = 10f;
    private bool isUltimateActive = false;
    private float normalCooldownQ;
    private Camera playerCamera;

    // Ability references
    public AbilityBase<VoidPlayerController> VoidBall;
    public AbilityBase<VoidPlayerController> BlinkAbility;
    public AbilityBase<VoidPlayerController> VoidPerspective;

    // Collision layers
    public LayerMask targetableLayer;

    // State tracking
    private Vector3 blinkTargetPosition;
    private bool waitingForBallPlacement = false;

    // Bug-fix: Add flags to prevent multiple activations
    private bool isProcessingVoidBall = false;
    private bool isProcessingBlink = false;
    private bool isProcessingUltimate = false;

    new private void Awake()
    {
        base.Awake();

        // Setup ability actions
        VoidBall.activateAbility = VoidBallHostCheck;
        BlinkAbility.activateAbility = AbilityTwoAnimation;
        VoidPerspective.activateAbility = VoidPerspectiveHostCheck;

        // Setup level up actions
        VoidBall.abilityLevelUp = VoidBallLevelUp;
        BlinkAbility.abilityLevelUp = BlinkLevelUp;
        VoidPerspective.abilityLevelUp = UltimateLevelUp;

        // Store base values
        normalCooldownQ = VoidBall.cooldown;
        baseAttackDamage = attackDamage;

        // Initialize multi-cast variable
        currentMaxCasts.Value = baseVoidBallCasts;

        // Character is a ranged attacker
        isMelee = false;
    }

    new private void Start()
    {
        base.Start();
        playerCamera = Camera.main;
        lastAbilityHitTime = Time.time;
    }

    new private void Update()
    {
        if (isStunned.Value)
        {
            isAttacking = false;
            animator.SetBool("AutoAttack", false);
            waitingForBallPlacement = false;
            remainingCasts = 0; // Reset casts if stunned
        }

        base.Update();
        if (!IsOwner || isDead.Value) return;

        // Check if passive should reset (no Q hits in the duration)
        if (Time.time - lastAbilityHitTime > passiveDuration && passiveStacks.Value > 0)
        {
            ResetPassiveServerRpc();
        }

        // Update damage based on passive
        UpdatePassiveDamage();

        // Ability use prevention during animations
        if (animator.GetBool("AbilityOne") == true)
        {
            BlinkAbility.preventAbilityUse = true;
            VoidPerspective.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == true)
        {
            VoidBall.preventAbilityUse = true;
            VoidPerspective.preventAbilityUse = true;
        }
        if (animator.GetBool("Ult") == true)
        {
            VoidBall.preventAbilityUse = false; // Allow Q during ultimate
            BlinkAbility.preventAbilityUse = true;
        }
        if (animator.GetBool("AutoAttack") == true)
        {
            VoidBall.preventAbilityUse = true;
            BlinkAbility.preventAbilityUse = true;
            VoidPerspective.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false &&
            animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
        {
            VoidPerspective.preventAbilityUse = false;
            VoidBall.preventAbilityUse = false;
            BlinkAbility.preventAbilityUse = false;
        }

        // Handle void ball placement when waiting for input
        if (waitingForBallPlacement && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;

            // Check if within range
            if (Vector3.Distance(transform.position, mousePosition) <= voidBallCastRange)
            {
                VoidBallPlaceServerRpc(new Vector2(mousePosition.x, mousePosition.y));

                // Decrement remaining casts
                remainingCasts--;

                // Continue waiting for placement if more casts remain
                if (remainingCasts <= 0)
                {
                    waitingForBallPlacement = false;
                }
                else
                {
                    // Show visual indicator for remaining casts
                    UpdateMultiCastUIClientRpc(remainingCasts);
                }
            }
        }
        else if (waitingForBallPlacement && Input.GetMouseButtonDown(1))
        {
            // Cancel ability on right-click
            waitingForBallPlacement = false;
            remainingCasts = 0;
            UpdateMultiCastUIClientRpc(0);
        }

        // Attempt abilities if not stunned
        if (!isStunned.Value)
        {
            // Handle ultimate activation
            if (Input.GetKeyDown(VoidPerspective.inputKey) &&
                VoidPerspective.isUnlocked &&
                !VoidPerspective.preventAbilityUse &&
                VoidPerspective.CanUse() &&
                !isProcessingUltimate)
            {
                VoidPerspectiveHostCheck();
            }

            // Special handling for void ball during ultimate
            if (isUltimateActive)
            {
                // Allow Q to be used more frequently during ultimate
                if (Input.GetKeyDown(VoidBall.inputKey) &&
                    VoidBall.isUnlocked &&
                    !VoidBall.preventAbilityUse &&
                    VoidBall.CanUse() &&
                    !waitingForBallPlacement &&
                    !isProcessingVoidBall)
                {
                    VoidBallHostCheck();
                }
            }
            else
            {
                // Handle void ball activation
                if (Input.GetKeyDown(VoidBall.inputKey) &&
                    VoidBall.isUnlocked &&
                    !VoidBall.preventAbilityUse &&
                    VoidBall.CanUse() &&
                    !waitingForBallPlacement &&
                    !isProcessingVoidBall)
                {
                    VoidBallHostCheck();
                }

                // Handle blink activation
                BlinkAbility.AttemptUse();
                /*if (Input.GetKeyDown(BlinkAbility.inputKey) &&
                    BlinkAbility.isUnlocked &&
                    !BlinkAbility.preventAbilityUse &&
                    BlinkAbility.CanUse() &&
                    !isProcessingBlink)
                {
                    BlinkHostCheck();
                }*/
            }
        }
    }

    private void UpdatePassiveDamage()
    {
        if (baseAttackDamage != BaseDamage.Value)
        {
            baseAttackDamage = BaseDamage.Value;
        }

        float damageAddition = (baseAttackDamage * (passiveDamageIncrease * passiveStacks.Value) + (baseUnaffectedDamage * passiveStacks.Value));
        float newDamage = baseAttackDamage + damageAddition;

        // Update the attack damage
        if (IsOwner)
        {
            UpdateAttackDamageServerRpc(newDamage);
        }
    }

    public void VoidBallHostCheck()
    {
        if (!IsOwner || isProcessingVoidBall) return;

        isProcessingVoidBall = true;

        // Initialize ability
        if (!isUltimateActive)
        {
            // Bug fix: Call OnUse() here to deduct mana only once
            VoidBall.OnUse();

            // Set up multi-cast for normal mode
            remainingCasts = currentMaxCasts.Value;
        }
        else
        {
            // Bug fix: Still need to deduct mana
            mana -= VoidBall.manaCost;
            VoidBall.lastUsed = Time.time;

            // Double the casts during ultimate
            remainingCasts = currentMaxCasts.Value * 2;
        }

        waitingForBallPlacement = true;

        AbilityOneAnimation();

        // Update UI to show available casts
        UpdateMultiCastUIClientRpc(remainingCasts);

        // Reset processing flag after a short delay
        StartCoroutine(ResetVoidBallProcessingFlag());
    }

    private IEnumerator ResetVoidBallProcessingFlag()
    {
        // Wait for a short time to prevent multiple activations
        yield return new WaitForSeconds(0.2f);
        isProcessingVoidBall = false;
    }

    [ClientRpc]
    private void UpdateMultiCastUIClientRpc(int remainingCasts)
    {
        // Update UI to show remaining casts
        // This would connect to your UI system
        Debug.Log($"Remaining Void Ball casts: {remainingCasts}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void VoidBallPlaceServerRpc(Vector2 position)
    {
        // Spawn danger circle indicator
        GameObject dangerCircle = Instantiate(dangerCirclePrefab, position, Quaternion.identity);
        var dangerCircleNet = dangerCircle.GetComponent<NetworkObject>();

        // Set size of danger circle
        //dangerCircle.transform.localScale = new Vector3(dangerCircleRadius * 2, dangerCircleRadius * 2, 1);

        // Make the danger circle visible to all clients immediately
        dangerCircleNet.Spawn();

        //bAM.PlayServerRpc("Void Ball Warning", new Vector3(position.x, position.y, 0));
        //bAM.PlayClientRpc("Void Ball Warning", new Vector3(position.x, position.y, 0));

        // Start the sequence to spawn the void ball after warning time
        StartCoroutine(SpawnVoidBallAfterWarning(position, dangerCircleNet));
    }
    private IEnumerator SpawnVoidBallAfterWarning(Vector2 position, NetworkObject dangerCircle)
    {
        // Wait for warning time
        yield return new WaitForSeconds(dangerCircleWarningTime);

        // Destroy danger circle
        dangerCircle.Despawn();
        Destroy(dangerCircle.gameObject);

        // Spawn the void ball
        GameObject voidBall = Instantiate(voidBallPrefab, position, Quaternion.identity);
        NetworkObject voidBallNet = voidBall.GetComponent<NetworkObject>();

        voidBallNet.Spawn(true);

        bAM.PlayServerRpc("Void Ball Launch", new Vector3(position.x, position.y, 0));
        bAM.PlayClientRpc("Void Ball Launch", new Vector3(position.x, position.y, 0));

        // Initialize ball properties
        VoidBallController ballController = voidBall.GetComponent<VoidBallController>();
        if (ballController != null)
        {
            ballController.Initialize(ballReturnSpeed, attackDamage * ballDamageMultiplier,
                                    new NetworkObjectReference(gameObject.GetComponent<NetworkObject>()),
                                    armorPen, this);

            // Explicitly sync the ball's state to all clients
            ballController.SyncInitialStateClientRpc(
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[0] } }); // Empty array means send to all
        }
    }

    public void BlinkHostCheck()
    {
        if (!IsOwner || isProcessingBlink) return;

        isProcessingBlink = true;

        // Get mouse position for blink direction
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPosition = transform.position;
        Vector2 blinkDirection = (mousePosition - playerPosition).normalized;

        // Calculate blink position
        blinkTargetPosition = playerPosition + (blinkDirection * blinkDistance);

        // Execute blink
        // Bug fix: Call OnUse() here to deduct mana only once
        //BlinkAbility.OnUse();
        //AbilityTwoAnimation();
        BlinkServerRpc(blinkTargetPosition);

        // Reset processing flag after a short delay
        StartCoroutine(ResetBlinkProcessingFlag());
    }

    private IEnumerator ResetBlinkProcessingFlag()
    {
        // Wait for a short time to prevent multiple activations
        yield return new WaitForSeconds(0.2f);
        isProcessingBlink = false;
    }

    [ServerRpc]
    private void BlinkServerRpc(Vector3 position)
    {
        // Play effects
        //bAM.PlayServerRpc("Void Blink", transform.position);
        //bAM.PlayClientRpc("Void Blink", transform.position);

        // Teleport to the position
        transform.position = position;

        // Update sprite direction based on blink
        if (position.x < transform.position.x)
        {
            SetSpriteDirectionServerRpc(true); // flip sprite to face left
        }
        else if (position.x > transform.position.x)
        {
            SetSpriteDirectionServerRpc(false); // sprite faces right
        }

        // Notify clients
        BlinkClientRpc(position);
    }

    [ClientRpc]
    private void BlinkClientRpc(Vector3 position)
    {
        // Update position on all clients
        transform.position = position;
    }

    public void VoidPerspectiveHostCheck()
    {
        if (!IsOwner || isProcessingUltimate) return;

        isProcessingUltimate = true;

        // Execute ultimate
        // Bug fix: Call OnUse() here to deduct mana only once
        VoidPerspective.OnUse();
        UltimateAnimation();
        VoidPerspectiveServerRpc();

        // Reset processing flag after a short delay
        StartCoroutine(ResetUltimateProcessingFlag());
    }

    private IEnumerator ResetUltimateProcessingFlag()
    {
        // Wait for a short time to prevent multiple activations
        yield return new WaitForSeconds(0.2f);
        isProcessingUltimate = false;
    }

    [ServerRpc]
    private void VoidPerspectiveServerRpc()
    {
        Debug.Log("Void Ultimate is happening!");
        bAM.PlayServerRpc("Void Ultimate", transform.position);
        bAM.PlayClientRpc("Void Ultimate", transform.position);

        isUltimateActive = true;
        health.invulnerable = true;

        TriggerBuffServerRpc("Speed", -maxSpeed * movementSpeedReduction, ultimateDuration, true);

        VoidBall.cooldown = normalCooldownQ * (1 - abilityCooldownReduction);

        float originalRange = voidBallCastRange;
        voidBallCastRange *= 10f;

        SetCameraZoomClientRpc(cameraZoomOutMultiplier);

        IEnumerator coroutine = UltimateDuration(originalRange);
        StartCoroutine(coroutine);
    }

    [ClientRpc]
    private void SetCameraZoomClientRpc(float zoomMultiplier)
    {
        if (IsOwner && playerCamera != null)
        {
            playerCamera.orthographicSize *= zoomMultiplier;
        }
    }

    public IEnumerator UltimateDuration(float originalRange)
    {
        yield return new WaitForSeconds(ultimateDuration);

        yield return new WaitUntil(() => (animator.GetBool("AbilityTwo") == false &&
                                        animator.GetBool("AbilityOne") == false &&
                                        animator.GetBool("Ult") == false &&
                                        animator.GetBool("AutoAttack") == false));

        health.invulnerable = false;
        isUltimateActive = false;
        VoidBall.cooldown = normalCooldownQ;
        voidBallCastRange = originalRange;

        SetCameraZoomClientRpc(1f / cameraZoomOutMultiplier);
    }

    // When a void ball hits an enemy
    public void OnVoidBallHit()
    {
        // Update last hit time for passive
        lastAbilityHitTime = Time.time;

        // Increment passive stacks
        IncrementPassiveServerRpc();
    }

    [ServerRpc]
    private void IncrementPassiveServerRpc()
    {
        passiveStacks.Value++;

        // Update UI or effects to show stacks
        UpdatePassiveStacksClientRpc(passiveStacks.Value);
    }

    [ServerRpc]
    private void ResetPassiveServerRpc()
    {
        passiveStacks.Value = 0;

        // Update UI or effects to show reset
        UpdatePassiveStacksClientRpc(0);
    }

    [ClientRpc]
    private void UpdatePassiveStacksClientRpc(int stacks)
    {
        // Update UI elements showing passive stacks
        // This would connect to your UI system
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
    private void SetSpriteDirectionServerRpc(bool flipX)
    {
        SetSpriteDirectionClientRpc(flipX);
    }

    [ClientRpc]
    private void SetSpriteDirectionClientRpc(bool flipX)
    {
        PlayerSprite.flipX = flipX;
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
            VoidBallLevelUp();
        }
        if (abilityNumber == 2)
        {
            BlinkLevelUp();
        }
        if (abilityNumber == 3)
        {
            UltimateLevelUp();
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

        // Improve passive
        passiveDamageIncrease += 0.02f; // Increase to 12%, 14%, etc.
        passiveDuration += 1f; // Increase duration by 1 second per level
    }

    public void VoidBallLevelUp()
    {
        if (!VoidBall.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            VoidBall.isUnlocked = true;
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
                VoidBall.abilityLevel++;
            }
            else
            {
                VoidBall.abilityLevel++;
                SyncAbilityLevelServerRpc(1);
            }

            if (VoidBall.abilityLevel == 2)
            {
                ballDamageMultiplier += 0.2f; // Increase damage
            }
            if (VoidBall.abilityLevel == 3)
            {
                dangerCircleWarningTime -= 0.5f; // Faster warning time
                UpdateMultiCastMaxServerRpc(currentMaxCasts.Value + 1);
            }
            if (VoidBall.abilityLevel == 4)
            {
                VoidBall.cooldown -= 2f; // Reduce cooldown
            }
            if (VoidBall.abilityLevel == 5)
            {
                // Add multi-cast at max level
                passiveDamageIncrease += level5PassiveIncrease;
            }
        }

        // Update stored normal cooldown for Q
        normalCooldownQ = VoidBall.cooldown;
    }

    [ServerRpc]
    private void UpdateMultiCastMaxServerRpc(int newMaxCasts)
    {
        currentMaxCasts.Value = newMaxCasts;
        SyncMultiCastMaxClientRpc(newMaxCasts);
    }

    [ClientRpc]
    private void SyncMultiCastMaxClientRpc(int newMaxCasts)
    {
        // Update UI if needed
        Debug.Log($"Max Void Ball casts updated to: {newMaxCasts}");
    }

    public void BlinkLevelUp()
    {
        if (!BlinkAbility.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            BlinkAbility.isUnlocked = true;
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
                BlinkAbility.abilityLevel++;
            }
            else
            {
                BlinkAbility.abilityLevel++;
                SyncAbilityLevelServerRpc(2);
            }

            if (BlinkAbility.abilityLevel == 2)
            {
                BlinkAbility.cooldown -= 3f; // Reduce cooldown
            }
            if (BlinkAbility.abilityLevel == 3)
            {
                BlinkAbility.cooldown -= 3f; // Reduce cooldown
                UpdateMultiCastMaxServerRpc(currentMaxCasts.Value + 1);
            }
            if (BlinkAbility.abilityLevel == 4)
            {
                BlinkAbility.cooldown -= 3f; // Reduce cooldown
            }
            if (BlinkAbility.abilityLevel == 5)
            {
                blinkDistance += 1.5f; // Further increase distance
                passiveDamageIncrease += level5PassiveIncrease;
            }
        }
    }

    public void UltimateLevelUp()
    {
        if (!VoidPerspective.isUnlocked)
        {
            if (unspentUnlocks.Value <= 0) return;
            VoidPerspective.isUnlocked = true;
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
                VoidPerspective.abilityLevel++;
            }
            else
            {
                VoidPerspective.abilityLevel++;
                SyncAbilityLevelServerRpc(3);
            }

            if (VoidPerspective.abilityLevel == 2)
            {
                ultimateDuration += 3f; // Longer duration
            }
            if (VoidPerspective.abilityLevel == 3)
            {
                cameraZoomOutMultiplier += 0.25f; // Greater zoom
                // Add an additional cast at level 4 ultimate
                UpdateMultiCastMaxServerRpc(currentMaxCasts.Value + 1);
            }
            if (VoidPerspective.abilityLevel == 4)
            {
                VoidPerspective.cooldown -= 25f;
            }
            if (VoidPerspective.abilityLevel == 5)
            {
                passiveDamageIncrease += level5PassiveIncrease;
            }
        }
    }
}