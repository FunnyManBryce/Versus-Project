using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Tower : NetworkBehaviour
{
    public int Team;
    private LameManager lameManager;

    public GameObject tower;
    public NetworkVariable<float> Health = new NetworkVariable<float>(1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        lameManager = FindObjectOfType<LameManager>();
    }

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                if (Team == 1)
                {
                    lameManager.TowerDestroyedServerRPC(Team);
                }
                else
                {
                    lameManager.TowerDestroyedServerRPC(Team);
                }
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
