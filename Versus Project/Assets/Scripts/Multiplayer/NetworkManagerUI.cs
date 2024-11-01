using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private NetworkManager networkManagerScript;
    public GameObject playersInLobby;
    public TMP_Text playersInLobbyText;
    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerMaximum = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [SerializeField] private GameObject objectHostButton;
    [SerializeField] private GameObject objectClientButton;
    [SerializeField] private GameObject IPText;
    public GameObject networkManager;
    public GameObject networkManagerUI;
    
    private void Awake()
    {
        hostButton.onClick.AddListener(() => { //Host button creates a lobby and then deactivates all the lobby creating/joining UI
            NetworkManager.Singleton.StartHost();
            playersInLobby.SetActive(true);
            objectHostButton.SetActive(false);
            objectClientButton.SetActive(false);
            IPText.SetActive(false);
        });
        clientButton.onClick.AddListener(() => { //Lobby button joins a lobby and then deactivates all the lobby creating/joining UI
            NetworkManager.Singleton.StartClient();
            playersInLobby.SetActive(true);
            objectHostButton.SetActive(false);
            objectClientButton.SetActive(false);
            IPText.SetActive(false);
        });
    }

    public override void OnNetworkSpawn()
    {
        totalPlayers.OnValueChanged += (int previousValue, int newValue) => //Whenever a new player joins a lobby, PlayersInLobby keeps track of it
        {
            playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value;
            Debug.Log(OwnerClientId + "; total players: " + totalPlayers.Value);
        };
        PlayerJoinedServerRPC();
        if(totalPlayers.Value >= playerMaximum.Value) //if there are too many players in the lobby, kick out the most recently joined one
        {
            DisconnectClientServerRPC(networkManagerScript.LocalClientId);
        }
    }

    private void Start() 
    {
        networkManagerScript.OnClientDisconnectCallback += PlayerDisconnctedServerRPC; //Makes it so PlayerDisconnectedServerRPC triggers whenever a client disconnects
    }
 
    public void IPString(string s) //Sets IP to whatever is typed into the IP box
    {
        networkManager.GetComponent<UnityTransport>().ConnectionData.Address = s; 
        Debug.Log(networkManager.GetComponent<UnityTransport>().ConnectionData.Address);
    }

    [Rpc(SendTo.Server)] //Sends info that a new player has joined to the server. This is then syncronized accross all the clients, since that is how network variables work
    public void PlayerJoinedServerRPC()
    {
        totalPlayers.Value++;      
    }
    [Rpc(SendTo.Server)] //sends info that a client has disconnected to the server. This is then syncronized accross all the clients, since that is how network variables work
    public void PlayerDisconnctedServerRPC(ulong clientID)
    {
        totalPlayers.Value--;
    }
    [Rpc(SendTo.Server)] //Disconnects a client based on their clientID
    public void DisconnectClientServerRPC(ulong clientID)
    {
        networkManagerScript.DisconnectClient(clientID);
    }
}
