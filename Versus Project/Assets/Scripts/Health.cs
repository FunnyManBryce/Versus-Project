using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> maxHealth = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Team = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float markedValue = 1f;
    public float startingMaxHealth; //Changing network variables in the inspector doesn't work, so this is the variable to change in order to change starting health
    public float armor = 0f; //In order for this to sync up to the baseplayercontroller, just do something like armor = baseplayercontroller.armor
    public bool initialValuesSynced;
    public bool invulnerable;
    public bool healthSetManual;


    public NetworkObjectReference lastAttacker;
    public override void OnNetworkSpawn()
    {
        if(IsServer && healthSetManual == false)
        {
            maxHealth.Value = startingMaxHealth;
            currentHealth.Value = startingMaxHealth;
        }
        initialValuesSynced = true;
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender, float armorPen) 
    {
        lastAttacker = sender;
        if(invulnerable == false)
        {
            if (sender.TryGet(out NetworkObject attacker))
            {
                if(markedValue != 1f)
                {
                    damage = damage * markedValue;
                } 
                float effectiveArmor = armor * (1 - (attacker != null ? armorPen / 100f : 0f));
                float damageReduction = 100f - (10000f / (100f + effectiveArmor));
                float reducedDamage = damage * (1f - (damageReduction / 100f));

                currentHealth.Value -= reducedDamage;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void HealServerRPC(float amount, NetworkObjectReference sender)
    {
        if (sender.TryGet(out NetworkObject healer))
        {
            currentHealth.Value += Mathf.Min(currentHealth.Value + amount, maxHealth.Value);
        }
    }
}
