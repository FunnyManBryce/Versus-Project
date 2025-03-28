using Unity.Netcode;
using UnityEngine;

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
    public bool isNPC;
    public bool healthSetManual;
    public bool darknessEffect;
    private float darknessTick = 0.5f;
    public NetworkObjectReference lastAttacker;

    public void Update()
    {
        if(darknessEffect && darknessTick > 0)
        {
            darknessTick -= Time.deltaTime;
        } else if(darknessEffect && darknessTick <= 0)
        {
            currentHealth.Value -= 10;
            darknessTick = 0.5f;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer && healthSetManual == false)
        {
            maxHealth.Value = startingMaxHealth;
            currentHealth.Value = startingMaxHealth;
        }
        initialValuesSynced = true;
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender, float armorPen, bool reducedNPCDamage)
    {
        lastAttacker = sender;
        if (invulnerable == false)
        {
            if (sender.TryGet(out NetworkObject attacker))
            {
                if (markedValue != 1f)
                {
                    damage = damage * markedValue;
                }
                float effectiveArmor = armor * (1 - (attacker != null ? armorPen / 100f : 0f));
                float damageReduction = 100f - (10000f / (100f + effectiveArmor));
                float reducedDamage = damage * (1f - (damageReduction / 100f));

                if (isNPC && reducedNPCDamage)
                {
                    currentHealth.Value -= reducedDamage / 2;
                }
                else
                {
                    currentHealth.Value -= reducedDamage;
                }
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

    [ServerRpc(RequireOwnership = false)]
    public void InflictBuffServerRpc(NetworkObjectReference Target, string buffType, float amount, float duration, bool hasDuration)
    {
        if (Target.TryGet(out NetworkObject targetObj))
        {
            if (targetObj.GetComponent<BasePlayerController>() != null)
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
            if (targetObj.GetComponent<MidBoss>() != null)
            {
                targetObj.GetComponent<MidBoss>().TriggerBuffServerRpc(buffType, amount, duration);
            }
        }
    }
}
