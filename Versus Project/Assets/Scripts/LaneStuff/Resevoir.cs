using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Resevoir : NetworkBehaviour
{
    public int Team;
    private LameManager lameManager;
    public GameObject teamPlayer;
    public GameObject resevoir;
    public NetworkObject networkResevoir;
    public NetworkObject networkReference;

    // Start is called before the first frame update
    void Start()
    {
        networkResevoir = resevoir.GetComponent<NetworkObject>();
    }

    private void OnTriggerStay(Collider target)
    {
        Debug.Log("resevoir working?");
        if(IsServer && target.tag == "Player")
        {
            networkReference = target.GetComponent<NetworkObject>();
            DealDamageServerRPC(-1, networkReference, networkResevoir);
        }
    }

    [Rpc(SendTo.Server)]
    public void DealDamageServerRPC(float damage, NetworkObjectReference reference, NetworkObjectReference sender)
    {
        if (reference.TryGet(out NetworkObject target))
        {
            target.GetComponent<BasePlayerController>().TakeDamageServerRpc(damage, sender);
        }
        else
        {
            Debug.Log("This is bad");
        }
    }
}
