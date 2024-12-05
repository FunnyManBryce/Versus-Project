using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Inhibitor : NetworkBehaviour
{
    public int Team;
    private LameManager lameManager;

    public Animator animator;


    public GameObject inhibitor;
    public NetworkObject networkInhibitor;

    public NetworkVariable<float> Health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    void Start()
    {
        networkInhibitor = inhibitor.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
        Health.Value = 100;
    }

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += (float previousValue, float newValue) => //Checking if dead
        {
            if (Health.Value <= 0 && IsServer == true)
            {
                lameManager.TowerDestroyedServerRPC(Team);
                if(Team == 1)
                {
                    lameManager.teamOneInhibAlive = false;
                } else if(Team == 2)
                {
                    lameManager.teamTwoInhibAlive = false;
                }
                inhibitor.GetComponent<NetworkObject>().Despawn();
            }
        };
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRPC(float damage, NetworkObjectReference sender)
    {
        Health.Value = Health.Value - damage;
    }

}
