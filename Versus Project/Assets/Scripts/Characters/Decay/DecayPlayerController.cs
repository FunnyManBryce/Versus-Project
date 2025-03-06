using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using static UnityEngine.GraphicsBuffer;

public class DecayPlayerController : BasePlayerController
{
    public float lastDecayTime;
    public float decayAmount;
    public NetworkVariable<float> totalStatDecay = new NetworkVariable<float>();
    public GameObject Decay;

    public int passiveLevel;
    public float timeToDecay = 30f;
    public bool statLossIsAOE = false;

    public GameObject AOEPrefab;
    public float AOEDamageMultiplier;
    public float speedReductionDuration = 5f;
    public bool AOESpeedSteal = false;

    public float shockwaveDamageMultiplier;
    public GameObject shockwavePrefab;
    public float shockWaveDuration = 2f;
    public bool immobilizeShockwave = false;

    public float ultimateDuration = 10;
    public bool ultSpeedIncrease = false;
    private NetworkObject ultTarget;

    public AbilityBase<DecayPlayerController> AOE;
    public AbilityBase<DecayPlayerController> Shockwave;
    public AbilityBase<DecayPlayerController> Ultimate;


    new private void Awake()
    {
        base.Awake();
        AOE.activateAbility = AOEServerRpc;
        Shockwave.activateAbility = AbilityTwoAnimation;
        Ultimate.activateAbility = UltimateServerRpc;
        AOE.abilityLevelUp = AOELevelUp;
        Shockwave.abilityLevelUp = ShockwaveLevelUp;
        Ultimate.abilityLevelUp = UltimateLevelUp;
    }

    [Rpc(SendTo.Server)]
    public void AOEServerRpc()
    {
        bAM.PlayServerRpc("Decay AOE", Decay.transform.position);
        bAM.PlayClientRpc("Decay AOE", Decay.transform.position);
        var AOE = Instantiate(AOEPrefab, Decay.transform.position, Quaternion.identity);
        AOE.GetComponent<DecayAOE>().damagePerTick = attackDamage * AOEDamageMultiplier;
        AOE.GetComponent<DecayAOE>().reductionDuration = speedReductionDuration;
        AOE.GetComponent<DecayAOE>().team = teamNumber.Value;
        AOE.GetComponent<DecayAOE>().sender = Decay.GetComponent<NetworkObject>();
        var AOENetworkObject = AOE.GetComponent<NetworkObject>();
        AOENetworkObject.SpawnWithOwnership(clientID);
        AOE.transform.SetParent(Decay.transform);
    }

    public void ShockwaveHostCheck()
    {
        if (!IsOwner) return;
        ShockwaveServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void ShockwaveServerRpc()
    {
        var shockwave = Instantiate(shockwavePrefab, Decay.transform.position, Quaternion.identity);
        shockwave.GetComponent<DecayShockWaveProjectile>().lifespan = shockWaveDuration;
        shockwave.GetComponent<DecayShockWaveProjectile>().damage = attackDamage * shockwaveDamageMultiplier;
        shockwave.GetComponent<DecayShockWaveProjectile>().team = teamNumber.Value;
        shockwave.GetComponent<DecayShockWaveProjectile>().sender = Decay.GetComponent<NetworkObject>();
        var shockwaveNetworkObject = shockwave.GetComponent<NetworkObject>();
        shockwaveNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.Server)]
    public void UltimateServerRpc()
    {
        Debug.Log("Ult is happening!");
        Vector2 pos = new Vector2(Decay.transform.position.x, Decay.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 6);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Attack Damage", -totalStatDecay.Value, ultimateDuration, true);
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor", -totalStatDecay.Value, ultimateDuration, true);
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor Pen", -totalStatDecay.Value, ultimateDuration, true);
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Mana Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
            }
        }
        TriggerBuffServerRpc("Attack Damage", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Armor", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Armor Pen", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Regen", (0.05f * totalStatDecay.Value), ultimateDuration, true);
        TriggerBuffServerRpc("Mana Regen", (0.05f * totalStatDecay.Value), ultimateDuration, true);
        if (ultSpeedIncrease)
        {
            TriggerBuffServerRpc("Speed", 3f, ultimateDuration, true);
        }
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= timeToDecay)
        {
            lastDecayTime = currentTime;
            TrackStatDecayServerRpc();
        }
        Ultimate.AttemptUse();
        AOE.AttemptUse();
        Shockwave.AttemptUse();
    }

    [Rpc(SendTo.Server)]
    private void TrackStatDecayServerRpc()
    {
        totalStatDecay.Value += 1;
        TriggerBuffServerRpc("Attack Damage", -decayAmount, 0, false);
        TriggerBuffServerRpc("Armor", -decayAmount, 0, false);
        TriggerBuffServerRpc("Armor Pen", -decayAmount, 0, false);
        TriggerBuffServerRpc("Regen", (0.05f * -decayAmount), 0, false);
        TriggerBuffServerRpc("Mana Regen", (0.05f * -decayAmount), 0, false);
        if (statLossIsAOE)
        {
            Vector2 pos = new Vector2(Decay.transform.position.x, Decay.transform.position.y);
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 5);
            foreach (var collider in hitColliders)
            {
                if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
                {
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Attack Damage", -decayAmount, 30, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor", -decayAmount, 30, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor Pen", -decayAmount, 30, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Regen", (0.05f * -decayAmount), 30, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Mana Regen", (0.05f * -decayAmount), 30, true);
                }
            }
        }
    }

    //Ability Level Up Effects
    #region
    [ServerRpc(RequireOwnership = false)]
    public void SyncAbilityLevelServerRpc(int abilityNumber)
    {
        if (abilityNumber == 0)
        {
            PassiveLevelUp();
        }
        if (abilityNumber == 1)
        {
            AOELevelUp();
        }
        if (abilityNumber == 2)
        {
            ShockwaveLevelUp();
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
            passiveLevel++;
        }
        else
        {
            passiveLevel++;
            SyncAbilityLevelServerRpc(0);
        }
        if (passiveLevel == 2)
        {
            decayAmount -= 0.2f;
        }
        if (passiveLevel == 3)
        {
            statLossIsAOE = true;
        }
        if (passiveLevel == 4)
        {
            decayAmount -= 0.2f;
            timeToDecay -= 7.5f;
        }
        if (passiveLevel == 5)
        {
            decayAmount -= 0.2f;
            timeToDecay -= 7.5f;
        }
    }

    public void AOELevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            AOE.abilityLevel++;
        }
        else
        {
            AOE.abilityLevel++;
            SyncAbilityLevelServerRpc(1);
        }
        if (AOE.abilityLevel == 2)
        {
            AOEDamageMultiplier += 0.5f;
        }
        if (AOE.abilityLevel == 3)
        {
            AOESpeedSteal = true;
        }
        if (AOE.abilityLevel == 4)
        {
            AOE.manaCost -= 10;
        }
        if (AOE.abilityLevel == 5)
        {
            speedReductionDuration += 2.5f;
        }
    }
    public void ShockwaveLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            Shockwave.abilityLevel++;
        }
        else
        {
            Shockwave.abilityLevel++;
            SyncAbilityLevelServerRpc(2);
        }
        if (Shockwave.abilityLevel == 2)
        {
            shockWaveDuration += 0.5f;
        }
        if (Shockwave.abilityLevel == 3)
        {
            immobilizeShockwave = true;
        }
        if (Shockwave.abilityLevel == 4)
        {
            shockwaveDamageMultiplier += 0.5f;
        }
        if (Shockwave.abilityLevel == 5)
        {
            Shockwave.manaCost -= 15;
        }
    }

    public void UltimateLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            Ultimate.abilityLevel++;
        }
        else
        {
            Ultimate.abilityLevel++;
            SyncAbilityLevelServerRpc(3);
        }
        if (Ultimate.abilityLevel == 2)
        {
            ultSpeedIncrease = true;
        }
        if (Ultimate.abilityLevel == 3)
        {
            Ultimate.cooldown -= 10;
        }
        if (Ultimate.abilityLevel == 4)
        {
            ultimateDuration += 2.5f;
        }
        if (Ultimate.abilityLevel == 5)
        {
            Ultimate.cooldown -= 10;
        }
    }
    #endregion

    [ServerRpc]
    public void InflictBuffServerRpc(NetworkObjectReference Target, string buffType, float amount, float duration, bool hasDuration)
    {
        if (Target.TryGet(out NetworkObject targetObj))
        {
            if(targetObj.GetComponent<BasePlayerController>() != null)
            {
                targetObj.GetComponent<BasePlayerController>().TriggerBuffServerRpc(buffType, amount, duration, hasDuration);
            }
            if (targetObj.GetComponent<MeleeMinion>() != null)
            {
                targetObj.GetComponent<MeleeMinion>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<Puppet>() != null)
            {
                targetObj.GetComponent<Puppet>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<JungleEnemy>() != null)
            {
                targetObj.GetComponent<JungleEnemy>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<Tower>() != null)
            {
                targetObj.GetComponent<Tower>().TriggerBuffServerRpc(buffType, amount, duration);
            }
        }
    }
}




