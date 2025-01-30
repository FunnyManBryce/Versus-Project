using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DecayPlayerController : BasePlayerController
{
    public float lastDecayTime;
    public float decayAmount;
    public NetworkVariable<float> totalStatDecay = new NetworkVariable<float>();
    public GameObject Decay;

    public GameObject AOEPrefab;
    public float shockwaveDamage;
    public GameObject shockwavePrefab;
    BasePlayerController enemyPlayer;
    public AbilityBase<DecayPlayerController> AOE;
    public AbilityBase<DecayPlayerController> Shockwave;
    public AbilityBase<DecayPlayerController> Ultimate;


    new private void Awake()
    {
        base.Awake();
        AOE.activateAbility = AOEServerRpc;
        Shockwave.activateAbility = ShockwaveServerRpc;
        Ultimate.activateAbility = UltimateServerRpc;
    }

    [Rpc(SendTo.Server)]
    public void AOEServerRpc()
    {
        var AOE = Instantiate(AOEPrefab, Decay.transform.position, Quaternion.identity);
        AOE.GetComponent<DecayAOE>().team = teamNumber.Value;
        AOE.GetComponent<DecayAOE>().sender = Decay.GetComponent<NetworkObject>();
        var AOENetworkObject = AOE.GetComponent<NetworkObject>();
        AOENetworkObject.SpawnWithOwnership(clientID);
        AOE.transform.SetParent(Decay.transform);
    }

    [Rpc(SendTo.Server)]
    public void ShockwaveServerRpc()
    {
        Vector2 pos = new Vector2(Decay.transform.position.x, Decay.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 2.5f);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(shockwaveDamage, new NetworkObjectReference(Decay.GetComponent<NetworkObject>()), armorPen);
            }
        }
        var shockwave = Instantiate(shockwavePrefab, Decay.transform.position, Quaternion.identity);
        shockwave.GetComponent<DecayShockWaveProjectile>().team = teamNumber.Value;
        shockwave.GetComponent<DecayShockWaveProjectile>().sender = Decay.GetComponent<NetworkObject>();
        var shockwaveNetworkObject = shockwave.GetComponent<NetworkObject>();
        shockwaveNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.Server)]
    public void UltimateServerRpc()
    {
        if (health.Team.Value == 1)
        {
            enemyPlayer = lameManager.playerTwoChar.GetComponent<BasePlayerController>();
        }
        else if (health.Team.Value == 2)
        {
            enemyPlayer =  lameManager.playerOneChar.GetComponent<BasePlayerController>();
        }
        enemyPlayer.TriggerBuffServerRpc("Attack Damage", -totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Armor", -totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Auto Attack Speed", -(0.1f * totalStatDecay.Value), 10f);
        enemyPlayer.TriggerBuffServerRpc("Armor Pen", -totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Regen", -(0.05f * totalStatDecay.Value), 10f);
        enemyPlayer.TriggerBuffServerRpc("Mana Regen", -(0.05f * totalStatDecay.Value), 10f);
        TriggerBuffServerRpc("Attack Damage", totalStatDecay.Value, 10f);
        TriggerBuffServerRpc("Armor", totalStatDecay.Value, 10f);
        TriggerBuffServerRpc("Auto Attack Speed", (0.1f * totalStatDecay.Value), 10f);
        TriggerBuffServerRpc("Armor Pen", totalStatDecay.Value, 10f);
        TriggerBuffServerRpc("Regen", (0.05f * totalStatDecay.Value), 10f);
        TriggerBuffServerRpc("Mana Regen", (0.05f * totalStatDecay.Value), 10f);
        TriggerBuffServerRpc("Speed", 3f, 10f);
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        AOE.AttemptUse();
        Shockwave.AttemptUse();
        Ultimate.AttemptUse();
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= 30f)
        {
            StatDecay();
            if(!IsServer)
            {
                TrackStatDecayServerRpc();
            }
        }
    }
   
    public void StatDecay()
    {
        if(IsServer)
        {
            totalStatDecay.Value += decayAmount;
        }
        lastDecayTime = lameManager.matchTimer.Value;
        attackDamage -= decayAmount;
        if (attackDamage <= 1f)
        {
            attackDamage = 1f;
        }
        autoAttackSpeed -= 0.1f * decayAmount;
        if (autoAttackSpeed <= 0.1f)
        {
            autoAttackSpeed = 0.1f;
        }
        health.armor -= decayAmount;
        if (health.armor <= 1f)
        {
            health.armor = 1f;
        }
        armorPen -= decayAmount;
        if (armorPen <= 1f)
        {
            armorPen = 1f;
        }
        regen -= 0.05f * decayAmount;
        if (regen <= 0.1f)
        {
            regen = 0.1f;
        }
        manaRegen -= 0.05f * decayAmount;
        if (manaRegen <= .1f)
        {
            manaRegen = 0.1f;
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectServerRpc(NetworkObjectReference networkObject, ulong clientID)
    {
        if (networkObject.TryGet(out NetworkObject Obj))
        {
            Obj.SpawnWithOwnership(clientID);
        }
    }

    [Rpc(SendTo.Server)]
    private void TrackStatDecayServerRpc()
    {
        totalStatDecay.Value += decayAmount;
        attackDamage -= decayAmount;
        if (attackDamage <= 1f)
        {
            attackDamage = 1f;
        }
        autoAttackSpeed -= 0.1f * decayAmount;
        if (autoAttackSpeed <= 0.1f)
        {
            autoAttackSpeed = 0.1f;
        }
        health.armor -= decayAmount;
        if (health.armor <= 1f)
        {
            health.armor = 1f;
        }
        armorPen -= decayAmount;
        if (armorPen <= 1f)
        {
            armorPen = 1f;
        }
        regen -= 0.05f * decayAmount;
        if (regen <= 0.1f)
        {
            regen = 0.1f;
        }
        manaRegen -= 0.05f * decayAmount;
        if (manaRegen <= .1f)
        {
            manaRegen = 0.1f;
        }
    }

}




