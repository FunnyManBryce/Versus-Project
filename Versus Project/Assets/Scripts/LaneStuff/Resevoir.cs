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

    // Start is called before the first frame update
    void Start()
    {
        networkResevoir = resevoir.GetComponent<NetworkObject>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("resevoir working?");
        if(IsServer && other.tag == "Player")
        {
            other.GetComponent<BasePlayerController>().resevoirRegen = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("resevoir working?");
        if (IsServer && other.tag == "Player")
        {
            other.GetComponent<BasePlayerController>().resevoirRegen = false;
        }
    }
}
