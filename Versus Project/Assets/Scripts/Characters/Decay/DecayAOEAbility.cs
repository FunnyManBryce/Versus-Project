/*using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class DecayAOEAbility : Ability<DecayPlayerController>
{
    public GameObject AOEPrefab;
    [Rpc(SendTo.Server)]
    public override void OnUse()
    {
        base.OnUse();
        var AOE = Object.Instantiate(AOEPrefab, playerController.Decay.transform.position, Quaternion.identity);
        AOE.GetComponent<DecayAOE>().team = playerController.teamNumber.Value;
        AOE.GetComponent<DecayAOE>().sender = playerController.Decay.GetComponent<NetworkObject>();
        var AOENetworkObject = AOE.GetComponent<NetworkObject>();
        AOENetworkObject.SpawnWithOwnership(playerController.clientID);
        AOE.transform.SetParent(playerController.Decay.transform);
    }
}*/
