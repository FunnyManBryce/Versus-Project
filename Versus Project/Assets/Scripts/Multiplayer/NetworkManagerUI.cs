using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : NetworkBehaviour
{
    public GameObject playersInLobby;
    public TMP_Text playersInLobbyText;
    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int testValue = 0;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    public GameObject networkManager;
    public GameObject networkManagerUI;
    
    private void Awake()
    {
        /*networkManagerUI = GameObject.Find("NetworkManagerUI");
        Transform UItransform = networkManagerUI.transform;
        UItransform.GetComponent<NetworkObject>().Spawn(true); */
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            playersInLobby.SetActive(true);
            lobbyClientRpc();

            //lobbyClientRpc();
            //totalPlayers.Value++;
            //playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value;
        });
        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            playersInLobby.SetActive(true);
            lobbyClientRpc();
            //lobbyServerRpc(new ServerRpcParams());
            //totalPlayers.Value++;
            //playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value;
        });
    }

    /*public override void OnNetworkSpawn()
    {
        totalPlayers.OnValueChanged += (int previousValue, int newValue) =>
        {
            playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value;
            Debug.Log(OwnerClientId + "; total players: " + totalPlayers.Value);
        };
    }*/

    private void Update()
    {
        
        //Debug.Log(totalPlayers.Value);
    }

    public void IPString(string s)
    {
        networkManager.GetComponent<UnityTransport>().ConnectionData.Address = s;
        Debug.Log(networkManager.GetComponent<UnityTransport>().ConnectionData.Address);
    }

    [ServerRpc]
    private void lobbyServerRpc()
    {
        totalPlayers.Value++;
        Debug.Log(totalPlayers.Value);

    }

    [ClientRpc]
    private void lobbyClientRpc()
    {
        totalPlayers.Value++;
        Debug.Log(totalPlayers.Value);
    }
}
