using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PuppeteeringPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public GameObject Puppet;
    public GameObject currentPuppet;
    public bool puppetSpawned;
    public NetworkVariable<bool> puppetAlive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> puppetDeathTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (IsOwner)
        {
            if (puppetAlive.Value == false && puppetSpawned == false)
            {
                float currentTime = lameManager.matchTimer.Value;
                if (currentTime - puppetDeathTime.Value >= 15f)
                {
                    PuppetSpawnServerRpc(teamNumber.Value, attackDamage, maxSpeed);
                }
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
        if(puppetAlive.Value == false)
        {
            puppetAlive.Value = true;
            currentPuppet = Instantiate(Puppet, gameObject.transform.position, Quaternion.identity);
            currentPuppet.GetComponent<Puppet>().Team = team;
            currentPuppet.GetComponent<Puppet>().Father = gameObject;
            currentPuppet.GetComponent<Puppet>().Damage = 1.5f * damage;
            currentPuppet.GetComponent<Puppet>().moveSpeed = 1f * speed;
            var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
            puppetNetworkObject.SpawnWithOwnership(clientID);
        }
    }
}
