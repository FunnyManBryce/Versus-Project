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

    //Abilities
    /*public float abilityOneCooldown;
    public float abilityTwoCooldown;
    public float ultimateCooldown;
    public float abilityOneManaCost;
    public float abilityTwoManaCost;
    public float ultimateCooldown; */

    new private void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner) return;
        if (Input.GetKey(KeyCode.R))
        {
            UltimateServerRpc(lameManager.playerTwoChar.GetComponent<NetworkObject>());
        }
        if (Input.GetKey(KeyCode.Q))
        {
            AOESummonServerRpc();
        }
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= 10f)
        {
            StatDecay();
        }
    }

    public void StatDecay()
    {
        lastDecayTime = lameManager.matchTimer.Value;
        totalStatDecay += decayAmount;
        attackDamage -= decayAmount;
        autoAttackSpeed -= 0.1f * decayAmount;
        health.armor -= decayAmount;
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

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject attacker))
        {
            if (attacker.GetComponent<BasePlayerController>() != null)
            {
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Attack Damage", -totalStatDecay, 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor", -totalStatDecay, 10f);
            }
        }
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Attack Damage", totalStatDecay, 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor", totalStatDecay, 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", 3f, 10f);

    }
}  
