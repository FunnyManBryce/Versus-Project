using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class DecayPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public float lastDecayTime;
    public float decayAmount;
    public float totalStatDecay;
    public GameObject AOEObject;
    public GameObject Decay;

    new private void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
    }

    new private void Update()
    {
        base.Update();
        if (!IsServer) return;
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= 10f)
        {
            StatDecay();
            AOESummonServerRpc();
        }
    }

    public void StatDecay()
    {
        lastDecayTime = lameManager.matchTimer.Value;
        totalStatDecay += decayAmount;
        attackDamage -= decayAmount;
        autoAttackSpeed -= 0.1f * decayAmount;
        armor -= decayAmount;
        armorPen -= decayAmount;
        regen -= 0.05f * decayAmount;
        maxMana -= 5f * decayAmount;
        manaRegen -= 0.05f * decayAmount;
    }

    [Rpc(SendTo.Server)]
    private void AOESummonServerRpc()
    {
        var AOE = Instantiate(AOEObject, Decay.transform.position, Quaternion.identity);
        AOE.GetComponent<DecayAOE>().team = teamNumber.Value;
        AOE.GetComponent<DecayAOE>().sender = Decay.GetComponent<NetworkObject>();
        var AOENetworkObject = AOE.GetComponent<NetworkObject>();
        AOENetworkObject.Spawn();
        AOE.transform.SetParent(Decay.transform);

    }

}
