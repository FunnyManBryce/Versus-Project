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

    // Visual effects
    public GameObject hitEffectPrefab;
    public TrailRenderer trailRenderer;

    // Target tracking
    private Vector3 targetPosition;
    private List<ulong> hitTargets = new List<ulong>();

    public void Initialize(float moveSpeed, float damageAmount, NetworkObjectReference caster, float armorPenetration, VoidPlayerController casterPlayer)
    {
        speed = moveSpeed;
        damage = damageAmount; 
        armorPen = armorPenetration;
        casterRef = caster;
        voidPlayer = casterPlayer;

        isReturning = false;

        // Begin return behavior after a short delay
        StartCoroutine(ReturnAfterDelay(0.01f));
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isReturning = true;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isReturning)
        {
            // Calculate return path to caster
            if (casterRef.TryGet(out NetworkObject casterObj))
            {
                Vector3 direction = (casterObj.transform.position - transform.position).normalized;
                transform.position += direction * speed * Time.deltaTime;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

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

                    // Get current damage from the VoidPlayerController
                    float currentDamage = damage;
                    if (voidPlayer != null)
                    {
                        // Use the voidPlayer's current attackDamage * ballDamageMultiplier
                        currentDamage = voidPlayer.attackDamage * voidPlayer.ballDamageMultiplier;
                    }

                    health.TakeDamageServerRPC(currentDamage, casterRef, armorPen, false);

                    if (hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                        NetworkObject netObj = hitEffect.GetComponent<NetworkObject>();
                        netObj.Spawn();

                        StartCoroutine(DestroyAfterDelay(hitEffect, 1.0f));
                    }

                    // Notify void player of hit for passive
                    if (voidPlayer != null)
                    {
                        voidPlayer.OnVoidBallHit();
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
    [ClientRpc]
    public void SyncVisualsClientRpc()
    {
        // Ensure trail renderer is visible and properly configured on all clients
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
            trailRenderer.emitting = true;
        }

        // Make sure any other visual components are properly enabled
        // If you have other renderers that might be affected:
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
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