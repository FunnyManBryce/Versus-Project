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

    public GameObject AOEPrefab;
    public float AOEDamageMultiplier;
    public float speedReductionDuration = 5f;
    public bool AOESpeedSteal = false;

    public float shockwaveDamageMultiplier;
    public GameObject shockwavePrefab;
    public float shockWaveDuration = 2f;
    public bool immobilizeShockwave = false;

    public RuntimeAnimatorController UltAnimator;
    public RuntimeAnimatorController NormalAnimator;
    public float ultimateDuration = 10;
    public bool ultSpeedIncrease = false;
    private NetworkObject ultTarget;

    public AbilityBase<DecayPlayerController> AOE;
    public AbilityBase<DecayPlayerController> Shockwave;
    public AbilityBase<DecayPlayerController> Ultimate;


    new private void Awake()
    {
        base.Awake();
        AOE.activateAbility = AbilityOneAnimation;
        Shockwave.activateAbility = AbilityTwoAnimation;
        Ultimate.activateAbility = UltimateAnimation;
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

    public void AOEHostCheck()
    {
        if (!IsOwner) return;
        AOEServerRpc();
    }

    public void UltimateHostCheck()
    {
        if (!IsOwner) return;
        UltimateServerRpc();
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
        animator.runtimeAnimatorController = UltAnimator;
        UltAnimChangeClientRpc();
        IEnumerator coroutine = UltimateDuration();
        StartCoroutine(coroutine);
        Vector2 pos = new Vector2(Decay.transform.position.x, Decay.transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 10);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Attack Damage", -totalStatDecay.Value, ultimateDuration, true);
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor", -totalStatDecay.Value, ultimateDuration, true);
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Armor Pen", -totalStatDecay.Value, ultimateDuration, true);
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
                health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Mana Regen", -(0.05f * totalStatDecay.Value), ultimateDuration, true);
                if (ultSpeedIncrease)
                {
                    health.InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Speed", -2f, ultimateDuration, true);
                }
            }
        }
        TriggerBuffServerRpc("Attack Damage", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Armor", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Armor Pen", totalStatDecay.Value, ultimateDuration, true);
        TriggerBuffServerRpc("Regen", (totalStatDecay.Value), ultimateDuration, true);
        TriggerBuffServerRpc("Mana Regen", (totalStatDecay.Value), ultimateDuration, true);
        if (ultSpeedIncrease)
        {
            TriggerBuffServerRpc("Speed", 2f, ultimateDuration, true);
        }
    }

    public IEnumerator UltimateDuration() 
    {
        yield return new WaitForSeconds(ultimateDuration);
        yield return new WaitUntil(() => (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false));
        animator.runtimeAnimatorController = NormalAnimator;
        UltEndAnimChangeClientRpc();
    }

    [ClientRpc]
    public void UltAnimChangeClientRpc()
    {
        animator.runtimeAnimatorController = UltAnimator;
    }
    [ClientRpc]
    public void UltEndAnimChangeClientRpc()
    {
        animator.runtimeAnimatorController = NormalAnimator;
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        if (animator.GetBool("AbilityOne") == true)
        {
            Ultimate.preventAbilityUse = true;
            Shockwave.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == true)
        {
            Ultimate.preventAbilityUse = true;
            AOE.preventAbilityUse = true;
        }
        if (animator.GetBool("Ult") == true)
        {
            AOE.preventAbilityUse = true;
            Shockwave.preventAbilityUse = true;
        }
        if (animator.GetBool("AutoAttack") == true)
        {
            Ultimate.preventAbilityUse = true;
            AOE.preventAbilityUse = true;
            Shockwave.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
        {
            Ultimate.preventAbilityUse = false;
            AOE.preventAbilityUse = false;
            Shockwave.preventAbilityUse = false;
        }
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
        BaseDamage.Value -= decayAmount;
        BaseArmor.Value -= decayAmount;
        BaseArmorPen.Value -= decayAmount;
        BaseRegen.Value -= (0.05f * decayAmount);
        BaseManaRegen.Value -= (0.05f * decayAmount);
    }

    //Ability Level Up Effects
    #region
    [ServerRpc(RequireOwnership = false)]
    public void SyncAbilityLevelServerRpc(int abilityNumber)
    {
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

    public void AOELevelUp()
    {
        if (!AOE.isUnlocked)
        {
            AOE.isUnlocked = true;
        }
        else
        {
            if (unspentUpgrades.Value <= 0 || AOE.abilityLevel == 5) return;
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
                AOEDamageMultiplier += 0.25f;
            }
            if (AOE.abilityLevel == 3)
            {
                AOESpeedSteal = true;
            }
            if (AOE.abilityLevel == 4)
            {
                AOE.cooldown -= 10f;
            }
            if (AOE.abilityLevel == 5)
            {
                speedReductionDuration += 5f;
            }
        }
    }
    public void ShockwaveLevelUp()
    {
        if (!Shockwave.isUnlocked)
        {
            Shockwave.isUnlocked = true;
        }
        else
        {
            if (unspentUpgrades.Value <= 0 || Shockwave.abilityLevel == 5) return;
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
                Shockwave.cooldown -= 1f;
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
    }

    public void UltimateLevelUp()
    {
        if (!Ultimate.isUnlocked)
        {
            Ultimate.isUnlocked = true;
        }
        else
        {
            if (unspentUpgrades.Value <= 0 || Ultimate.abilityLevel == 5) return;
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
                Ultimate.cooldown -= 10;
            }
            if (Ultimate.abilityLevel == 5)
            {
                ultimateDuration += 8;
            }
        }
    }
    #endregion

}