using Unity.Netcode;
using UnityEngine;

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
        if (IsServer && other.tag == "Player" && other.GetComponent<BasePlayerController>().teamNumber.Value == Team)
        {
            other.GetComponent<BasePlayerController>().resevoirRegen.Value = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsServer && other.tag == "Player" && other.GetComponent<BasePlayerController>().teamNumber.Value == Team)
        {
            other.GetComponent<BasePlayerController>().resevoirRegen.Value = false;
        }
    }
}
