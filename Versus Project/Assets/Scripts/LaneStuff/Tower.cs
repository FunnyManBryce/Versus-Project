using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Tower : NetworkBehaviour
{
    public int Team;
    private LameManager lameManager;

    public NetworkVariable<float> Health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameObject tower;
    
    void Start()
    {
        lameManager = FindObjectOfType<LameManager>();
        //var towerToSpawn = tower.GetComponent<NetworkObject>();
        //towerToSpawn.Spawn();
        Health.Value = 100;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Iaminagony");
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                lameManager.TowerDestroyedServerRPC(Team);
                tower.GetComponent<NetworkObject>().Despawn();
            }
        };
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender)
    {
        Health.Value = Health.Value - damage;
        Debug.Log("Tower is in pain");
    }
}
