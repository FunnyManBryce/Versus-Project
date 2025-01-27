using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PuppeteeringPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public GameObject puppetPrefab;
    public List<GameObject> PuppetList;
    public NetworkVariable<int> puppetsAlive = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxPuppets = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> puppetDeathTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameObject stringObject;
    public bool defensiveMode;
    public AbilityBase<PuppeteeringPlayerController> String;
    public AbilityBase<PuppeteeringPlayerController> ModeSwitch;
    public AbilityBase<PuppeteeringPlayerController> Ultimate;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
        String.activateAbility = StringSummonServerRpc;
        ModeSwitch.activateAbility = PuppetModeSwitchServerRpc;
        Ultimate.activateAbility = UltimateServerRpc;
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (!IsOwner) return;
        String.AttemptUse();
        ModeSwitch.AttemptUse();
        Ultimate.AttemptUse();
        if (puppetsAlive.Value < maxPuppets.Value)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - puppetDeathTime.Value >= 15f)
            {
                PuppetSpawnServerRpc(teamNumber.Value, attackDamage, maxSpeed);
            }
        } else
        {
            if(currentTarget != null)
            {
                SyncPuppetValuesServerRpc(currentTarget);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
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
            PuppetSpawnServerRpc(team, attackDamage, maxSpeed);
        }
    }

    [Rpc(SendTo.Server)]
    private void PuppetSpawnServerRpc(int team, float damage, float speed)
    {
        if(puppetsAlive.Value < maxPuppets.Value)
        {
            puppetsAlive.Value++;
            GameObject currentPuppet = Instantiate(puppetPrefab, gameObject.transform.position, Quaternion.identity);
            PuppetList.Add(currentPuppet);
            currentPuppet.GetComponent<Puppet>().Team = team;
            currentPuppet.GetComponent<Puppet>().health.Team.Value = team;
            currentPuppet.GetComponent<Puppet>().Father = gameObject;
            currentPuppet.GetComponent<Puppet>().Damage = 1.5f * damage;
            currentPuppet.GetComponent<Puppet>().moveSpeed = 1f * speed;
            var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
            puppetNetworkObject.Spawn();
        }
    }

    [Rpc(SendTo.Server)]
    private void SyncPuppetValuesServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObj))
        {
            currentTarget = targetObj;
        }
    }

    [Rpc(SendTo.Server)]
    private void StringSummonServerRpc()
    {
        var String = Instantiate(stringObject, gameObject.transform.position, Quaternion.identity);
        String.GetComponent<StringAbility>().team = teamNumber.Value;
        String.GetComponent<StringAbility>().sender = gameObject.GetComponent<NetworkObject>();
        var StringNetworkObject = String.GetComponent<NetworkObject>();
        StringNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.Server)]
    private void PuppetModeSwitchServerRpc()
    {
        foreach(GameObject puppet in PuppetList)
        {
            puppet.GetComponent<Puppet>().defensiveMode = !defensiveMode;
            defensiveMode = puppet.GetComponent<Puppet>().defensiveMode;
            if (defensiveMode == true) //Switching to defensive mode buffs defense
            {
                TriggerBuffServerRpc("Armor", 10, 5f);
                TriggerBuffServerRpc("Regen", 15, 5f);
                TriggerBuffServerRpc("Speed", 2, 5f);

            }
            else // Switching to offensive mode buffs offense
            {
                TriggerBuffServerRpc("Attack Damage", 3, 5f);
                TriggerBuffServerRpc("Armor Pen", 5, 5f);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Armor Pen", 10, 5f);
                puppet.GetComponent<Puppet>().TriggerBuffServerRpc("Attack Damage", 10, 5f);

            }
        }
    }

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc()
    {

    }

}
