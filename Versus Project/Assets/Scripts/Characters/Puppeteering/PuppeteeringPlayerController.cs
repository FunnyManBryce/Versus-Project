using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

public class PuppeteeringPlayerController : BasePlayerController
{
    public GameObject puppetPrefab;
    public List<GameObject> PuppetList;
    public bool manaTax;
    public NetworkVariable<int> puppetsAlive = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxPuppets = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> puppetDeathTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> lastUltTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> ultActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float puppetRespawnLength = 20f;
    public float puppetStartingHealth = 175;
    public float puppetSpeedMultiplier = 1.0f;
    public float puppetCooldown = 2.0f;
    public float puppetRegen = 5f;

    public GameObject stringObject;
    public int passiveLevel;
    public float stringDamageMultiplier;
    public float stringMarkValue;

    public float armorBuffMultiplier;
    public float attackBuffMultiplier;
    public float pierceBuffMultiplier;
    public float lifestealMultiplier;

    public float ultimateDuration = 20f;

    public AbilityBase<PuppeteeringPlayerController> String;
    public AbilityBase<PuppeteeringPlayerController> ModeSwitch;
    public AbilityBase<PuppeteeringPlayerController> Ultimate;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        String.activateAbility = StringSummonServerRpc;
        ModeSwitch.activateAbility = PuppetModeSwitchServerRpc;
        Ultimate.activateAbility = UltimateServerRpc;
        String.abilityLevelUp = StringLevelUp;
        ModeSwitch.abilityLevelUp = ModeSwitchLevelUp;
        Ultimate.abilityLevelUp = UltimateLevelUp;

    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        String.AttemptUse();
        ModeSwitch.AttemptUse();
        Ultimate.AttemptUse();
        if(ultActive.Value == true)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - lastUltTime.Value >= ultimateDuration)
            {
                UltEndServerRpc();
            }
        }
        if (puppetsAlive.Value < maxPuppets.Value)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - puppetDeathTime.Value >= puppetRespawnLength)
            {
                PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, ultActive.Value);
            }
        } else if (puppetsAlive.Value == maxPuppets.Value)
        {
            if(currentTarget != null)
            {
                SyncPuppetValuesServerRpc(currentTarget);
            }
        } else if(puppetsAlive.Value > maxPuppets.Value)
        {
            PuppetDespawnServerRpc();
        }
    }

    public override void OnNetworkSpawn()
    {
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0 && isDead.Value == false && IsServer)
            {
                isDead.Value = true;
            }
        };
        XP.OnValueChanged += (float previousValue, float newValue) => //Checking for Level up
        {
            if (XP.Value >= XPToNextLevel && IsServer)
            {
                LevelUpServerRPC();
            }
        };
        isDead.OnValueChanged += (bool previousValue, bool newValue) => //Checking if dead
        {
            if (isDead.Value)
            {
                transform.position = new Vector3(-420, -69, 0);
                StartCoroutine(lameManager.PlayerDeath(gameObject.GetComponent<NetworkObject>(), lameManager.respawnLength.Value));
                if(puppetsAlive.Value >= 1)
                {
                    foreach (var puppet in PuppetList)
                    {
                        NetworkObject puppetToDespawn = PuppetList.Last().GetComponent<NetworkObject>();
                        GameObject lastPuppet = PuppetList.Last();
                        PuppetList.Remove(lastPuppet);
                        puppetsAlive.Value--;
                        puppetToDespawn.Despawn();
                    }
                }
            }
            else
            {
                transform.position = lameManager.playerSP[health.Team.Value - 1];
                health.currentHealth.Value = health.maxHealth.Value;
                mana = maxMana;
            }
        };
        puppetsAlive.OnValueChanged += (int previousValue, int newValue) => //Checking if dead
        {
            if (puppetsAlive.Value <= 0)
            {
                manaTax = false;
                manaRegen++;
            } else if(puppetsAlive.Value == 1 && manaTax == false)
            {
                manaTax = true;
                manaRegen--;
            }
        };
        if (IsOwner)
        {
            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);

            string canvasName = NetworkManager.LocalClientId == 0 ? "Player1UICanvas" : "Player2UICanvas";
            GameObject playerCanvas = GameObject.Find(canvasName);

            if (playerCanvas != null)
            {
                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;
            }
            PuppetSpawnServerRpc(team, attackDamage, maxSpeed, false);
        }

    }

    [Rpc(SendTo.Server)]
    private void PuppetSpawnServerRpc(int team, float damage, float speed, bool ultSpawn)
    {
        if(puppetsAlive.Value < maxPuppets.Value)
        {
            puppetsAlive.Value++;
            GameObject currentPuppet = Instantiate(puppetPrefab, gameObject.transform.position, Quaternion.identity);
            PuppetList.Add(currentPuppet);
            Puppet puppet = currentPuppet.GetComponent<Puppet>();
            puppet.Team = team;
            puppet.health.Team.Value = team;
            puppet.Father = gameObject;
            puppet.Damage = 1.5f * damage;
            puppet.moveSpeed = puppetSpeedMultiplier * speed;
            puppet.cooldownLength = puppetCooldown;
            puppet.lifestealMultiplier = lifestealMultiplier;
            puppet.regen = puppetRegen;
            puppet.health.healthSetManual = true;
            puppet.health.maxHealth.Value = puppetStartingHealth + (25 * Level.Value);
            puppet.health.currentHealth.Value = puppet.health.maxHealth.Value;
            var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
            puppetNetworkObject.Spawn();
            if(ultSpawn && puppetsAlive.Value > 1)
            {
                puppet.defensiveMode = !PuppetList[0].GetComponent<Puppet>().defensiveMode;
            } else
            {
                puppet.defensiveMode = false;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SyncPuppetValuesServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObj))
        {
            currentTarget = targetObj; //should maybe have stats sync each second???? could go wrong though
        }
    }

    [Rpc(SendTo.Server)]
    private void StringSummonServerRpc()
    {
        var String = Instantiate(stringObject, gameObject.transform.position, Quaternion.identity);
        String.GetComponent<StringAbility>().damage = attackDamage * stringDamageMultiplier;
        String.GetComponent<StringAbility>().markAmount = stringMarkValue;
        String.GetComponent<StringAbility>().team = health.Team.Value;
        String.GetComponent<StringAbility>().sender = gameObject.GetComponent<NetworkObject>();
        var StringNetworkObject = String.GetComponent<NetworkObject>();
        StringNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.Server)]
    private void PuppetModeSwitchServerRpc()
    {
        foreach(GameObject puppet in PuppetList)
        {
            puppet.GetComponent<Puppet>().defensiveMode = !puppet.GetComponent<Puppet>().defensiveMode;
            if (puppet.GetComponent<Puppet>().defensiveMode == true) //Switching to defensive mode buffs defense
            {
                TriggerBuffServerRpc("Armor", armorBuffMultiplier * health.armor, 5f, true);
                TriggerBuffServerRpc("Regen", 10, 5f, true);
                TriggerBuffServerRpc("Speed", 2, 5f, true);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Speed", 2, 5f);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Armor", armorBuffMultiplier * puppet.GetComponent<Puppet>().health.armor, 5f);

            }
            else // Switching to offensive mode buffs offense
            {
                TriggerBuffServerRpc("Attack Damage", attackBuffMultiplier * attackDamage, 5f, true);
                TriggerBuffServerRpc("Armor Pen", pierceBuffMultiplier * armorPen, 5f, true);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Armor Pen", pierceBuffMultiplier * puppet.GetComponent<Puppet>().armorPen, 5f);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Attack Damage", attackBuffMultiplier * puppet.GetComponent<Puppet>().Damage, 5f);

            }
        }
    }

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc()
    {
        maxPuppets.Value++;
        PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, true);
        lastUltTime.Value = lameManager.matchTimer.Value;
        ultActive.Value = true;
    }

    [Rpc(SendTo.Server)]
    private void UltEndServerRpc()
    {
        if(ultActive.Value == true)
        {
            ultActive.Value = false;
            maxPuppets.Value--;
        }
    }

    [Rpc(SendTo.Server)]
    private void PuppetDespawnServerRpc()
    {
        if (puppetsAlive.Value > maxPuppets.Value)
        {
            NetworkObject puppetToDespawn = PuppetList.Last().GetComponent<NetworkObject>();
            GameObject lastPuppet = PuppetList.Last();
            PuppetList.Remove(lastPuppet);
            puppetsAlive.Value--;
            puppetToDespawn.Despawn();
        }
    }

    public void PassiveLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        passiveLevel++;
        if (passiveLevel == 2)
        {
            puppetStartingHealth += 50;
        }
        if (passiveLevel == 3)
        {
            puppetRespawnLength = puppetRespawnLength - 5;
        }
        if (passiveLevel == 4)
        {
            puppetSpeedMultiplier += 0.3f;
        }
        if (passiveLevel == 5)
        {
            puppetCooldown -= 0.5f;
        }
    }

    public void StringLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return; //Will need to make a server Rpc that changes unspent upgrades value and ability level for client and server
        String.abilityLevel++;
        if(String.abilityLevel == 2)
        {
            //Upgrade Effect
        }
        if (String.abilityLevel == 3)
        {
            stringMarkValue = 0.33f;
        }
        if (String.abilityLevel == 4)
        {
            stringDamageMultiplier = stringDamageMultiplier + 0.667f;
        }
        if (String.abilityLevel == 5)
        {
            //Upgrade Effect
        }
    }
    public void ModeSwitchLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return; //Will need to make a server Rpc that changes unspent upgrades value and ability level for client and server
        ModeSwitch.abilityLevel++;
        if (ModeSwitch.abilityLevel == 2)
        {
            lifestealMultiplier += 0.2f;
            puppetRegen += 5f;
        }
        if (ModeSwitch.abilityLevel == 3)
        {
            //Upgrade Effect
        }
        if (ModeSwitch.abilityLevel == 4)
        {
            lifestealMultiplier += 0.2f;
            puppetRegen += 10f;
        }
        if (ModeSwitch.abilityLevel == 5)
        {
            ModeSwitch.manaCost -= 10;
        }
    }

    public void UltimateLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return; //Will need to make a server Rpc that changes unspent upgrades value and ability level for client and server
        Ultimate.abilityLevel++;
        if (Ultimate.abilityLevel == 2)
        {
            ultimateDuration += 5;
        }
        if (Ultimate.abilityLevel == 3)
        {
            //Upgrade Effect
        }
        if (Ultimate.abilityLevel == 4)
        {
            Ultimate.cooldown -= 10;
            Ultimate.manaCost -= 10;
        }
        if (Ultimate.abilityLevel == 5)
        {
            //Upgrade Effect
        }
    }
}
