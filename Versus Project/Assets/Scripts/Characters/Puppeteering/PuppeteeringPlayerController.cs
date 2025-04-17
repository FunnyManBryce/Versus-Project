using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PuppeteeringPlayerController : BasePlayerController
{
    public GameObject puppetPrefab;
    public List<GameObject> PuppetList;
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
    public bool stringMoveReduction = false;
    public bool stringTargetsAll = false;

    public float armorBuffMultiplier;
    public float attackBuffMultiplier;
    public float pierceBuffMultiplier;
    public float lifestealMultiplier;

    public float ultimateDuration = 20f;
    private bool doubleUltSpawn = false;
    private bool ultInvuln = false;

    public AbilityBase<PuppeteeringPlayerController> String;
    public AbilityBase<PuppeteeringPlayerController> ModeSwitch;
    public AbilityBase<PuppeteeringPlayerController> Ultimate;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        String.activateAbility = AbilityOneAnimation;
        ModeSwitch.activateAbility = AbilityTwoAnimation;
        Ultimate.activateAbility = UltimateAnimation;
        String.abilityLevelUp = StringLevelUp;
        ModeSwitch.abilityLevelUp = ModeSwitchLevelUp;
        Ultimate.abilityLevelUp = UltimateLevelUp;

    }

    public void StringHostCheck()
    {
        if (!IsOwner) return;
        StringSummonServerRpc();
    }
    public void ModeSwitchHostCheck()
    {
        if (!IsOwner) return;
        PuppetModeSwitchServerRpc();
    }
    public void UltimateHostCheck()
    {
        if (!IsOwner) return;
        UltimateServerRpc();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (!IsOwner || isDead.Value) return;
        if (animator.GetBool("AbilityOne") == true)
        {
            Ultimate.preventAbilityUse = true;
            ModeSwitch.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == true)
        {
            Ultimate.preventAbilityUse = true;
            String.preventAbilityUse = true;
        }
        if (animator.GetBool("Ult") == true)
        {
            ModeSwitch.preventAbilityUse = true;
            String.preventAbilityUse = true;
        }
        if (animator.GetBool("AutoAttack") == true)
        {
            Ultimate.preventAbilityUse = true;
            String.preventAbilityUse = true;
            ModeSwitch.preventAbilityUse = true;
        }
        if (animator.GetBool("AbilityTwo") == false && animator.GetBool("AbilityOne") == false && animator.GetBool("Ult") == false && animator.GetBool("AutoAttack") == false)
        {
            Ultimate.preventAbilityUse = false;
            String.preventAbilityUse = false;
            ModeSwitch.preventAbilityUse = false;
        }
        String.AttemptUse();
        ModeSwitch.AttemptUse();
        Ultimate.AttemptUse();
        if (ultActive.Value == true)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - lastUltTime.Value >= ultimateDuration)
            {
                UltEndServerRpc();
            }
        }
        if (puppetsAlive.Value < maxPuppets.Value && !isDead.Value)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - puppetDeathTime.Value >= puppetRespawnLength)
            {
                PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "Normal");
            }
        }
        else if (puppetsAlive.Value > 0)
        {
            if (currentTarget != null)
            {
                SyncPuppetValuesServerRpc(currentTarget);
            }
        }
        if (puppetsAlive.Value > maxPuppets.Value)
        {
            PuppetDespawnServerRpc();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            BaseDamage.Value = attackDamage;
            BaseAttackSpeed.Value = autoAttackSpeed;
            BaseRange.Value = attackRange;
            BaseCDR.Value = cDR;
            BaseArmor.Value = health.armor;
            BaseArmorPen.Value = armorPen;
            BaseRegen.Value = regen;
            BaseManaRegen.Value = manaRegen;
            BaseSpeed.Value = maxSpeed;
        }
        health.currentHealth.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (health.currentHealth.Value <= 0 && isDead.Value == false && IsServer)
            {
                isDead.Value = true;
                if (health.lastAttacker.TryGet(out NetworkObject attacker))
                {
                    if (attacker.GetComponent<BasePlayerController>() != null)
                    {
                        var enemyPlayer = attacker.GetComponent<BasePlayerController>();
                        enemyPlayer.XP.Value += Level.Value * 50; //Can change the amount given later
                        enemyPlayer.Gold.Value += Level.Value * 50; //Can change the amount given later
                    }
                }
            }
        };
        XP.OnValueChanged += (float previousValue, float newValue) => //Checking for Level up
        {
            if (XP.Value >= XPToNextLevel.Value && IsServer)
            {
                LevelUpServerRPC();
            }
        };
        SuddenDeath.OnValueChanged += (bool previousValue, bool newValue) =>
        {
            if (SuddenDeath.Value)
            {
                if (teamNumber.Value == 1)
                {
                    transform.position = new Vector3(410, 70, 0);
                }
                else
                {
                    transform.position = new Vector3(440, 70, 0);
                }
                if (puppetsAlive.Value >= 1)
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
                PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "Normal");
                isDead.Value = false;
                health.currentHealth.Value = health.maxHealth.Value;
                mana = maxMana;
                attackDamage = BaseDamage.Value;
                autoAttackSpeed = BaseAttackSpeed.Value;
                attackRange = BaseRange.Value;
                cDR = BaseCDR.Value;
                health.armor = BaseArmor.Value;
                armorPen = BaseArmorPen.Value;
                regen = BaseRegen.Value;
                manaRegen = BaseManaRegen.Value;
                maxSpeed = BaseSpeed.Value;

                DamageBuff.Value = 0;
                AttackSpeedBuff.Value = 0;
                RangeBuff.Value = 0;
                CDRBuff.Value = 0;
                ArmorBuff.Value = 0;
                ArmorPenBuff.Value = 0;
                RegenBuff.Value = 0;
                ManaRegenBuff.Value = 0;
                SpeedBuff.Value = 0;
                appliesDarkness.Value = false;
                darknessDuration = 120;
                health.darknessEffect = false;

                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i] != null)
                    {
                        StopCoroutine(Buffs[i]);
                    }
                }
            }
        };
        isDead.OnValueChanged += (bool previousValue, bool newValue) => //Checking if dead
        {
            if (isDead.Value)
            {
                currentTarget = null;
                transform.position = new Vector3(-420, -69, 0);
                StartCoroutine(lameManager.PlayerDeath(gameObject.GetComponent<NetworkObject>(), lameManager.respawnLength.Value, OwnerClientId));
                if (IsServer)
                {
                    maxPuppets.Value = 1;
                }
                if (puppetsAlive.Value >= 1)
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
                attackDamage = BaseDamage.Value;
                autoAttackSpeed = BaseAttackSpeed.Value;
                attackRange = BaseRange.Value;
                cDR = BaseCDR.Value;
                health.armor = BaseArmor.Value;
                armorPen = BaseArmorPen.Value;
                regen = BaseRegen.Value;
                manaRegen = BaseManaRegen.Value;
                maxSpeed = BaseSpeed.Value;

                DamageBuff.Value = 0;
                AttackSpeedBuff.Value = 0;
                RangeBuff.Value = 0;
                CDRBuff.Value = 0;
                ArmorBuff.Value = 0;
                ArmorPenBuff.Value = 0;
                RegenBuff.Value = 0;
                ManaRegenBuff.Value = 0;
                SpeedBuff.Value = 0;
                appliesDarkness.Value = false;
                darknessDuration = 120;
                health.darknessEffect = false;

                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i] != null)
                    {
                        StopCoroutine(Buffs[i]);
                    }
                }
            }
            else
            {
                PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "Normal");
                health.currentHealth.Value = health.maxHealth.Value;
                mana = maxMana;
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
            PuppetSpawnServerRpc(team, attackDamage, maxSpeed, "Normal");
        }
        if (IsOwner)
        {
            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);
            Debug.Log("1");

            string canvasName = NetworkManager.LocalClientId == 0 ? "Player1UICanvas" : "Player2UICanvas";
            GameObject playerCanvas = GameObject.Find(canvasName);

            if (playerCanvas != null)
            {
                GameObject shop = Instantiate(shopPrefab, playerCanvas.transform);
                shop.GetComponent<Shop>().enabled = true;

                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;

                GameObject manaBar = Instantiate(manaBarPrefab, playerCanvas.transform);
                manaBar.GetComponent<PlayerManaBar>().enabled = true;

                GameObject xpBar = Instantiate(xpBarPrefab, playerCanvas.transform);
                xpBar.GetComponent<PlayerXPBar>().enabled = true;

                GameObject abilty1CooldownBar = Instantiate(abilty1CooldownBarPrefab, playerCanvas.transform);
                abilty1CooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject abilty2CooldownBar = Instantiate(abilty2CooldownBarPrefab, playerCanvas.transform);
                abilty2CooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject ultimateCooldownBar = Instantiate(UltimateCooldownBarPrefab, playerCanvas.transform);
                ultimateCooldownBar.GetComponent<PlayerCooldownBars>().enabled = true;

                GameObject goldDisplay = Instantiate(goldDisplayPrefab, playerCanvas.transform);
                goldDisplay.GetComponent<PlayerGoldDisplay>().enabled = true;

                GameObject AttackDisplay = Instantiate(attackDamagePrefab, playerCanvas.transform);
                AttackDisplay.GetComponent<PlayerDamageDisplay>().enabled = true;

                GameObject attackSpeedDisplay = Instantiate(attackSpeedDisplayPrefab, playerCanvas.transform);
                attackSpeedDisplay.GetComponent<PlayerAttackSpeedDisplay>().enabled = true;

                GameObject attackRangeDisplay = Instantiate(attackRangeDisplayPrefab, playerCanvas.transform);
                attackRangeDisplay.GetComponent<PlayerAttackRangeDisplay>().enabled = true;

                GameObject armorDisplay = Instantiate(armorDisplayPrefab, playerCanvas.transform);
                armorDisplay.GetComponent<PlayerArmorDisplay>().enabled = true;

                GameObject cDRDisplay = Instantiate(cDRDisplayPrefab, playerCanvas.transform);
                cDRDisplay.GetComponent<PlayerCDRDisplay>().enabled = true;

                GameObject armorPenDisplay = Instantiate(armorPenDisplayPrefab, playerCanvas.transform);
                armorPenDisplay.GetComponent<PlayerArmorPenDisplay>().enabled = true;

                GameObject moveSpeedDisplay = Instantiate(moveSpeedDisplayPrefab, playerCanvas.transform);
                moveSpeedDisplay.GetComponent<PlayerMoveSpeedDisplay>().enabled = true;

                GameObject regenDisplay = Instantiate(regenDisplayPrefab, playerCanvas.transform);
                regenDisplay.GetComponent<PlayerRegenDisplay>().enabled = true;

                GameObject manaRegenDisplay = Instantiate(manaRegenDisplayPrefab, playerCanvas.transform);
                manaRegenDisplay.GetComponent<PlayerManaRegenDisplay>().enabled = true;

            }
        }
        if (!IsOwner)
        {
            GameObject healthBar = Instantiate(enemyHealthBarPrefab, GameObject.Find("Enemy UI Canvas").transform);
            HealthBar = healthBar;
            healthBar.GetComponent<EnemyHealthBar>().enabled = true;
            healthBar.GetComponent<EnemyHealthBar>().SyncValues(gameObject, gameObject.transform, 1.5f);
        }
    }

    [Rpc(SendTo.Server)]
    private void PuppetSpawnServerRpc(int team, float damage, float speed, string spawnType)
    {
        if (puppetsAlive.Value < maxPuppets.Value && !isDead.Value)
        {
            puppetsAlive.Value++;
            GameObject currentPuppet = Instantiate(puppetPrefab, gameObject.transform.position, Quaternion.identity);
            PuppetList.Add(currentPuppet);
            Puppet puppet = currentPuppet.GetComponent<Puppet>();
            puppet.Team = team;
            puppet.health.Team.Value = team;
            puppet.Father = gameObject;
            puppet.Damage = 1f * damage;
            puppet.moveSpeed = puppetSpeedMultiplier * speed;
            puppet.cooldownLength = puppetCooldown;
            puppet.lifestealMultiplier = lifestealMultiplier;
            puppet.regen = puppetRegen;
            puppet.health.healthSetManual = true;
            if (Level.Value < 1)
            {
                puppet.health.maxHealth.Value = puppetStartingHealth + 25;
            }
            else if (Level.Value >= 1)
            {
                puppet.health.maxHealth.Value = puppetStartingHealth + (25 * Level.Value);
            }
            if (spawnType == "ModeSwitch")
            {
                puppet.health.currentHealth.Value = puppet.health.maxHealth.Value * ((lameManager.matchTimer.Value - puppetDeathTime.Value) / puppetRespawnLength);
            }
            else
            {
                puppet.health.currentHealth.Value = puppet.health.maxHealth.Value;
            }
            var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
            puppetNetworkObject.Spawn();
            if (spawnType == "ultSpawn" && puppetsAlive.Value > 1)
            {
                puppet.defensiveMode = !PuppetList[0].GetComponent<Puppet>().defensiveMode;
                if (ultInvuln)
                {
                    puppet.TriggerBuffServerRpc("Invulnerability", 0, 3);
                }
            }
            else
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
        foreach (GameObject puppet in PuppetList)
        {
            puppet.GetComponent<Puppet>().defensiveMode = !puppet.GetComponent<Puppet>().defensiveMode;
            if (puppet.GetComponent<Puppet>().defensiveMode == true) //Switching to defensive mode buffs defense
            {
                TriggerBuffServerRpc("Armor", armorBuffMultiplier * health.armor, 5f, true);
                TriggerBuffServerRpc("Regen", 10, 5f, true);
                TriggerBuffServerRpc("Speed", 2, 5f, true);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Armor", armorBuffMultiplier * puppet.GetComponent<Puppet>().health.armor, 5f);

            }
            else // Switching to offensive mode buffs offense
            {
                TriggerBuffServerRpc("Attack Damage", attackBuffMultiplier * attackDamage, 5f, true);
                TriggerBuffServerRpc("Armor Pen", pierceBuffMultiplier * armorPen, 5f, true);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Armor Pen", pierceBuffMultiplier * puppet.GetComponent<Puppet>().armorPen, 5f);
            }
        }
        if (puppetsAlive.Value == 0)
        {
            PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "ModeSwitch");
        }
    }

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc()
    {
        maxPuppets.Value = 2;
        PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "ultSpawn");
        if (doubleUltSpawn && puppetsAlive.Value == 0)
        {
            PuppetSpawnServerRpc(health.Team.Value, attackDamage, maxSpeed, "ultSpawn");
        }
        lastUltTime.Value = lameManager.matchTimer.Value;
        ultActive.Value = true;
    }

    [Rpc(SendTo.Server)]
    private void UltEndServerRpc()
    {
        if (ultActive.Value == true)
        {
            ultActive.Value = false;
            maxPuppets.Value = 1;
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
            StringLevelUp();
        }
        if (abilityNumber == 2)
        {
            ModeSwitchLevelUp();
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
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            String.abilityLevel++;
        }
        else
        {
            String.abilityLevel++;
            SyncAbilityLevelServerRpc(1);
        }
        if (String.abilityLevel == 2)
        {
            stringMoveReduction = true;
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
            stringTargetsAll = true;
        }
    }
    public void ModeSwitchLevelUp()
    {
        if (unspentUpgrades.Value <= 0) return;
        if (IsServer)
        {
            unspentUpgrades.Value--;
            ModeSwitch.abilityLevel++;
        }
        else
        {
            ModeSwitch.abilityLevel++;
            SyncAbilityLevelServerRpc(2);
        }
        if (ModeSwitch.abilityLevel == 2)
        {
            lifestealMultiplier += 0.2f;
            puppetRegen += 5f;
        }
        if (ModeSwitch.abilityLevel == 3)
        {
            ModeSwitch.cooldown -= 5;
        }
        if (ModeSwitch.abilityLevel == 4)
        {
            lifestealMultiplier += 0.3f;
            puppetRegen += 10f;
        }
        if (ModeSwitch.abilityLevel == 5)
        {
            ModeSwitch.manaCost -= 10;
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
            ultimateDuration += 5;
        }
        if (Ultimate.abilityLevel == 3)
        {
            doubleUltSpawn = true;
        }
        if (Ultimate.abilityLevel == 4)
        {
            Ultimate.cooldown -= 10;
            Ultimate.manaCost -= 10;
        }
        if (Ultimate.abilityLevel == 5)
        {
            ultInvuln = true;
        }
    }
    #endregion 
}
