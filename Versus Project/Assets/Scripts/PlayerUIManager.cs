using Unity.Netcode;
using UnityEngine;

public class PlayerUIManager : NetworkBehaviour
{
    [SerializeField] private GameObject player1UICanvas;
    [SerializeField] private GameObject player2UICanvas;

    public static PlayerUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializePlayerUI(ulong clientId)
    {
        if (!IsServer) return;

        GameObject targetCanvas = clientId == 0 ? player1UICanvas : player2UICanvas;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        SetupPlayerUIClientRpc(clientId == 0, clientRpcParams);
    }

    [ClientRpc]
    private void SetupPlayerUIClientRpc(bool isPlayer1, ClientRpcParams clientRpcParams)
    {
        player1UICanvas.SetActive(isPlayer1);
        player2UICanvas.SetActive(!isPlayer1);
    }
}