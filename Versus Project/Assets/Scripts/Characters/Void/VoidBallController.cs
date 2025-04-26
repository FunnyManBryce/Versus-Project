using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VoidBallController : NetworkBehaviour
{
    // Ball properties
    public float speed;
    public float damage;
    public float armorPen;
    private bool isReturning = false;
    private NetworkObjectReference casterRef;
    private VoidPlayerController voidPlayer;

    // Acceleration properties
    public float accelerationRate = 0.5f;    // Speed increases by this amount per second
    public float maxSpeedMultiplier = 3.0f;  // Maximum speed will be this times the initial speed
    private float initialSpeed;              // Store the initial speed
    private float travelTime = 0f;           // Track how long the ball has been traveling

    // Visual effects
    public GameObject hitEffectPrefab;
    public TrailRenderer trailRenderer;

    // Target tracking
    private Vector3 targetPosition;
    private List<ulong> hitTargets = new List<ulong>();

    public void Initialize(float moveSpeed, float damageAmount, NetworkObjectReference caster, float armorPenetration, VoidPlayerController casterPlayer)
    {
        speed = moveSpeed;
        initialSpeed = moveSpeed;  // Store initial speed for acceleration calculations
        damage = damageAmount;
        armorPen = armorPenetration;
        casterRef = caster;
        voidPlayer = casterPlayer;

        // Start as not returning
        isReturning = false;

        // Begin return behavior after a short delay
        StartCoroutine(ReturnAfterDelay(0.5f));
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isReturning = true;

        // Reset travel time when returning to maintain smooth acceleration
        travelTime = 0f;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Increment travel time
        travelTime += Time.deltaTime;

        // Calculate current speed based on travel time
        float currentSpeed = initialSpeed + (accelerationRate * travelTime);

        // Cap the speed at the maximum limit
        currentSpeed = Mathf.Min(currentSpeed, initialSpeed * maxSpeedMultiplier);

        // Apply the calculated speed
        if (isReturning)
        {
            // Calculate return path to caster
            if (casterRef.TryGet(out NetworkObject casterObj))
            {
                Vector3 direction = (casterObj.transform.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                // Scale the trail width based on speed for visual feedback
                if (trailRenderer != null)
                {
                    float speedRatio = currentSpeed / (initialSpeed * maxSpeedMultiplier);
                    trailRenderer.startWidth = Mathf.Lerp(trailRenderer.startWidth, trailRenderer.startWidth * 1.5f, speedRatio);
                }

                if (Vector3.Distance(transform.position, casterObj.transform.position) < 1.0f)
                {
                    NetworkObject.Despawn(true);
                }
            }
            else
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<Health>(out Health health))
        {
            NetworkObject targetObj = other.GetComponent<NetworkObject>();

            if (hitTargets.Contains(targetObj.NetworkObjectId))
                return;

            if (casterRef.TryGet(out NetworkObject casterObj) &&
                casterObj.TryGetComponent<BasePlayerController>(out BasePlayerController controller))
            {
                if (controller.CanAttackTarget(targetObj) && targetObj.NetworkObjectId != casterObj.NetworkObjectId)
                {
                    hitTargets.Add(targetObj.NetworkObjectId);

                    // Calculate bonus damage based on current speed
                    float currentSpeed = initialSpeed + (accelerationRate * travelTime);
                    float speedMultiplier = Mathf.Clamp(currentSpeed / initialSpeed, 1f, maxSpeedMultiplier);
                    float calculatedDamage = damage * speedMultiplier;

                    health.TakeDamageServerRPC(calculatedDamage, casterRef, armorPen, false);

                    if (hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                        NetworkObject netObj = hitEffect.GetComponent<NetworkObject>();
                        netObj.Spawn();

                        StartCoroutine(DestroyAfterDelay(hitEffect, 1.0f));
                    }

                    // Notify void player of hit for passive - passing the target object
                    if (voidPlayer != null)
                    {
                        voidPlayer.OnVoidBallHit(targetObj);
                    }

                    // Play sound effect
                    if (controller.bAM != null)
                    {
                        controller.bAM.PlayServerRpc("Void Ball Hit", transform.position);
                        controller.bAM.PlayClientRpc("Void Ball Hit", transform.position);
                    }
                }
            }
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            netObj.Despawn();
            Destroy(obj);
        }
    }
}