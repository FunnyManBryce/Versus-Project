using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PuppeteeringPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public GameObject Puppet;
    private GameObject currentPuppet;
    public bool puppetAlive;
    public float puppetDeathTime;
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
        if (!IsServer) return;
        if(puppetAlive == false)
        {
            float currentTime = lameManager.matchTimer.Value;
            if (currentTime - puppetDeathTime >= 15f)
            {
                PuppetSpawnServerRpc();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);
            Debug.Log("1");

            string canvasName = NetworkManager.LocalClientId == 0 ? "Player1UICanvas" : "Player2UICanvas";
            GameObject playerCanvas = GameObject.Find(canvasName);

            if (playerCanvas != null)
            {
                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;
            }
            PuppetSpawnServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void PuppetSpawnServerRpc()
    {
        puppetAlive = true;
        currentPuppet = Instantiate(Puppet, gameObject.transform.position, Quaternion.identity);
        currentPuppet.GetComponent<Puppet>().Team = teamNumber.Value;
        currentPuppet.GetComponent<Puppet>().Father = gameObject;
        currentPuppet.GetComponent<Puppet>().Damage = 1.5f * attackDamage;
        currentPuppet.GetComponent<Puppet>().moveSpeed = maxSpeed;
        var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
        puppetNetworkObject.SpawnWithOwnership(clientID);
        //currentPuppet.transform.SetParent(gameObject.transform);
    }
}
