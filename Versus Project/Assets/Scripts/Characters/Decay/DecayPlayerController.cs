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
    public float slamArea = 2.5f;
    public float shockWaveDuration = 2f;
    private bool immobilizeSlam = false;
    public bool immobilizeShockwave = false;

    BasePlayerController enemyPlayer;
    public float ultimateDuration = 10;
    public bool ultSpeedIncrease = false;

    public AbilityBase<DecayPlayerController> AOE;
    public AbilityBase<DecayPlayerController> Shockwave;
    public AbilityBase<DecayPlayerController> Ultimate;


    new private void Awake()
    {
        base.Awake();
        AOE.activateAbility = AOEServerRpc;
        Shockwave.activateAbility = ShockwaveServerRpc;
        Ultimate.activateAbility = UltimateServerRpc;
        AOE.abilityLevelUp = AOELevelUp;
        Shockwave.abilityLevelUp = ShockwaveLevelUp;
        Ultimate.abilityLevelUp = UltimateLevelUp;
    }

    [Rpc(SendTo.Server)]
    public void AOEServerRpc()
    {
        var AOE = Instantiate(AOEPrefab, Decay.transform.position, Quaternion.identity);
        AOE.GetComponent<DecayAOE>().damagePerTick = attackDamage * AOEDamageMultiplier;
        AOE.GetComponent<DecayAOE>().reductionDuration = speedReductionDuration;
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
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, slamArea);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(attackDamage * shockwaveDamageMultiplier, new NetworkObjectReference(Decay.GetComponent<NetworkObject>()), armorPen);
                if(immobilizeSlam)
                {
                    if (collider.GetComponent<BasePlayerController>() != null)
                    {
                        collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f, true);
                    }
                    if (collider.GetComponent<MeleeMinion>() != null)
                    {
                        collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    }
                    if (collider.GetComponent<JungleEnemy>() != null)
                    {
                        collider.GetComponent<JungleEnemy>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    }
                    if (collider.GetComponent<Puppet>() != null)
                    {
                        collider.GetComponent<Puppet>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    }
                }
            }
        }
        var shockwave = Instantiate(shockwavePrefab, Decay.transform.position, Quaternion.identity);
        shockwave.GetComponent<DecayShockWaveProjectile>().lifespan = shockWaveDuration;
        shockwave.GetComponent<DecayShockWaveProjectile>().damage = (attackDamage * shockwaveDamageMultiplier)/2;
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
        enemyPlayer.TriggerBuffServerRpc("Attack Damage", -totalStatDecay.Value, ultimateDuration, true);
        enemyPlayer.TriggerBuffServerRpc("Armor", -totalStatDecay.Value, ultimateDuration, true);
        enemyPlayer.TriggerBuffServerRpc("Auto Attack Speed", -(0.1f * totalStatDecay.Value), ultimateDuration, true);
        enemyPlayer.TriggerBuffServerRpc("Armor Pen", -totalStatDecay.Value, ultimateDuration, true);
        enemyPlayer.TriggerBuffServerRpc("Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
        enemyPlayer.TriggerBuffServerRpc("Mana Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
        TriggerBuffServerRpc("Attack Damage", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Armor", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Auto Attack Speed", (0.1f * totalStatDecay.Value), ultimateDuration, true);
        TriggerBuffServerRpc("Armor Pen", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Regen", (0.05f * totalStatDecay.Value), ultimateDuration, true);
        TriggerBuffServerRpc("Mana Regen", (0.05f * totalStatDecay.Value), ultimateDuration, true);
        if(ultSpeedIncrease)
        {
            TriggerBuffServerRpc("Speed", 3f, ultimateDuration, true);
        }
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        AOE.AttemptUse();
        Shockwave.AttemptUse();
        Ultimate.AttemptUse();
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= timeToDecay)
        {
            lastDecayTime = currentTime;
            TrackStatDecayServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void TrackStatDecayServerRpc()
    {
        totalStatDecay.Value += 1;
        TriggerBuffServerRpc("Attack Damage", -decayAmount, 0, false);
        TriggerBuffServerRpc("Armor", -decayAmount, 0, false);
        TriggerBuffServerRpc("Auto Attack Speed", -(0.05f * decayAmount), 0, false);
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
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Auto Attack Speed", (0.1f * -decayAmount), 30, true);
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
        unspentUpgrades.Value--;
        if (abilityNumber == 0)
        {
            passiveLevel++;
        }
        if (abilityNumber == 1)
        {
            AOE.abilityLevel++;
        }
        if (abilityNumber == 2)
        {
            Shockwave.abilityLevel++;
        }
        if (abilityNumber == 3)
        {
            Ultimate.abilityLevel++;
        }
    }
    [ClientRpc(RequireOwnership = false)]
    public void SyncAbilityLevelClientRpc(int abilityNumber)
    {
        if (abilityNumber == 0)
        {
            passiveLevel++;
        }
        if (abilityNumber == 1)
        {
            AOE.abilityLevel++;
        }
        if (abilityNumber == 2)
        {
            Shockwave.abilityLevel++;
        }
        if (abilityNumber == 3)
        {
            Ultimate.abilityLevel++;
        }
    }
    public void PassiveLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            passiveLevel++;
            SyncAbilityLevelClientRpc(0);
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
            SyncAbilityLevelClientRpc(1);
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
            SyncAbilityLevelClientRpc(2);
        }
        else
        {
            Shockwave.abilityLevel++;
            SyncAbilityLevelServerRpc(2);
        }
        if (Shockwave.abilityLevel == 2)
        {
            slamArea += 5;
            shockWaveDuration += 0.5f;
        }
        if (Shockwave.abilityLevel == 3)
        {
            immobilizeSlam = true;
        }
        if (Shockwave.abilityLevel == 4)
        {
            shockwaveDamageMultiplier += 0.25f;
        }
        if (Shockwave.abilityLevel == 5)
        {
            immobilizeShockwave = true;
        }
    }

    public void UltimateLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            Ultimate.abilityLevel++;
            SyncAbilityLevelClientRpc(3);
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




