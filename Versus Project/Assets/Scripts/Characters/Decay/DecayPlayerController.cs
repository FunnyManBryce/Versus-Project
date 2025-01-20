using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class DecayPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public float lastDecayTime;
    public float decayAmount;
    public NetworkVariable<float> totalStatDecay = new NetworkVariable<float>();
    public GameObject Decay;

    //Ability 1
    private bool isAOneOnCD;
    public float AOneManaCost;
    public float abilityOneCD;
    public GameObject AOEObject;

    //Ability 2
    private bool isATwoOnCD;
    public float ATwoManaCost;
    public float abilityTwoCD;
    public float shockwaveDamage;
    public GameObject shockwaveProjectile;

    //Ultimate Ability
    private bool isUltOnCD;
    public float ultManaCost;
    public float ultCD;

    new private void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
    }

    new private void Update()
    {
        base.Update();
        if (!IsOwner) return;
        if (Input.GetKey(KeyCode.Q) && isAOneOnCD == false && AOneManaCost <= mana)
        {
            isAOneOnCD = true;
            mana -= AOneManaCost;
            IEnumerator coroutine = CooldownTimer(abilityOneCD / (cDR / 2), 1);
            StartCoroutine(coroutine);
            AOESummonServerRpc();
        }
        if (Input.GetKey(KeyCode.E) && isATwoOnCD == false && ATwoManaCost <= mana)
        {
            isATwoOnCD = true;
            mana -= ATwoManaCost;
            IEnumerator coroutine = CooldownTimer(abilityTwoCD / (cDR / 2), 2);
            StartCoroutine(coroutine);
            ShockwaveSummonServerRpc();
        }
        if (Input.GetKey(KeyCode.R) && isUltOnCD == false && ultManaCost <= mana)
        {
            isUltOnCD = true;
            mana -= ultManaCost;
            IEnumerator coroutine = CooldownTimer(ultCD / (cDR / 2), 3);
            StartCoroutine(coroutine);
            if(teamNumber.Value == 1)
            {
                UltimateServerRpc(lameManager.playerTwoChar.GetComponent<NetworkObject>());
            }
            else if(teamNumber.Value == 2)
            {
                UltimateServerRpc(lameManager.playerOneChar.GetComponent<NetworkObject>());
            }
        }
        float currentTime = lameManager.matchTimer.Value;
        if(currentTime - lastDecayTime >= 10f)
        {
            StatDecay();
            TrackStatDecayServerRpc();
        }
    }

    public void StatDecay()
    {
        lastDecayTime = lameManager.matchTimer.Value;
        attackDamage -= decayAmount;
        autoAttackSpeed -= 0.1f * decayAmount;
        health.armor -= decayAmount;
        armorPen -= decayAmount;
        regen -= 0.05f * decayAmount;
        //maxMana -= 5f * decayAmount;
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
    private void ShockwaveSummonServerRpc()
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
        var shockwave = Instantiate(shockwaveProjectile, Decay.transform.position, Quaternion.identity);
        shockwave.GetComponent<DecayShockWaveProjectile>().team = teamNumber.Value;
        shockwave.GetComponent<DecayShockWaveProjectile>().sender = Decay.GetComponent<NetworkObject>();
        var shockwaveNetworkObject = shockwave.GetComponent<NetworkObject>();
        shockwaveNetworkObject.Spawn();
    }

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject attacker))
        {
            if (attacker.GetComponent<BasePlayerController>() != null)
            {
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Attack Damage", -totalStatDecay.Value, 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor", -totalStatDecay.Value, 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Auto Attack Speed", -(0.1f * totalStatDecay.Value), 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor Pen", -totalStatDecay.Value, 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Regen", -(0.05f * totalStatDecay.Value), 10f);
                attacker.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Mana Regen", -(0.05f * totalStatDecay.Value), 10f);
            }
        }
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Attack Damage", totalStatDecay.Value, 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor", totalStatDecay.Value, 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Auto Attack Speed", (0.1f * totalStatDecay.Value), 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Armor Pen", totalStatDecay.Value, 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Regen", (0.05f * totalStatDecay.Value), 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Mana Regen", (0.05f * totalStatDecay.Value), 10f);
        Decay.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", 3f, 10f);
    }

    [Rpc(SendTo.Server)]
    private void TrackStatDecayServerRpc()
    {
        totalStatDecay.Value += decayAmount;

    }

    public IEnumerator CooldownTimer(float duration, int abilityNumber) 
    {
        yield return new WaitForSeconds(duration);
        if(abilityNumber == 1)
        {
            isAOneOnCD = false;
        }
        else if (abilityNumber == 2)
        {
            isATwoOnCD = false;
        }
        else if (abilityNumber == 3)
        {
            isUltOnCD = false;
        }
        Debug.Log("Ability Off Cooldown");
    }
}  
