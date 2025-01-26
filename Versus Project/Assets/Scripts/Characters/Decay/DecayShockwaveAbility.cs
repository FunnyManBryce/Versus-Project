/*using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class DecayShockwaveAbiity : Ability<DecayPlayerController>
{
    public float shockwaveDamage;
    public GameObject shockwavePrefab;
    [Rpc(SendTo.Server)]
    public override void OnUse()
    {
        base.OnUse();
        Vector2 pos = new Vector2(playerController.Decay.transform.position.x, playerController.Decay.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 2.5f);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && playerController.CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(shockwaveDamage, new NetworkObjectReference(playerController.Decay.GetComponent<NetworkObject>()), playerController.armorPen);
            }
        }
        var shockwave = Object.Instantiate(shockwavePrefab, playerController.Decay.transform.position, Quaternion.identity);
        shockwave.GetComponent<DecayShockWaveProjectile>().team = playerController.teamNumber.Value;
        shockwave.GetComponent<DecayShockWaveProjectile>().sender = playerController.Decay.GetComponent<NetworkObject>();
        var shockwaveNetworkObject = shockwave.GetComponent<NetworkObject>();
        shockwaveNetworkObject.SpawnWithOwnership(playerController.clientID);
    }
}
*/