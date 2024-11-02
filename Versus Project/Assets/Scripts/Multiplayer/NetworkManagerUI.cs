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
    public NetworkVariable<int> charsSelected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> readyToStart = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [SerializeField] private GameObject lobbyCreationUI;
    [SerializeField] private GameObject lobbySelectionUI;
    [SerializeField] private GameObject charSelectionUI;
    [SerializeField] private GameObject IPText;
    public GameObject networkManager;
    public GameObject networkManagerUI;
    
    private void Awake()
    {
        hostButton.onClick.AddListener(() => { //Host button creates a lobby
            NetworkManager.Singleton.StartHost();
            lobbySelectionUI.SetActive(true);
        });
        clientButton.onClick.AddListener(() => { //Lobby button joins a lobby
            NetworkManager.Singleton.StartClient();
        });
    }

    public override void OnNetworkSpawn()
    {
        lobbyCreationUI.SetActive(false);
        charSelectionUI.SetActive(true);
        playersInLobby.SetActive(true);
        totalPlayers.OnValueChanged += (int previousValue, int newValue) => //Whenever a new player joins a lobby, PlayersInLobby keeps track of it
        {
            playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value + "/" + playerMaximum.Value;
            Debug.Log(OwnerClientId + "; total players: " + totalPlayers.Value);
        };
        playerMaximum.OnValueChanged += (int previousValue, int newValue) => //Text to keep track of the max amount of players in the lobby
        {
            playersInLobbyText.text = "Players in Lobby: " + totalPlayers.Value + "/" + playerMaximum.Value;
            Debug.Log(OwnerClientId + "; PlayerMaximum changed to: " + playerMaximum.Value);
        };
        charsSelected.OnValueChanged += (int previousValue, int newValue) => //if everyone has selected a character, the game can start
        {
            if(charsSelected.Value == playerMaximum.Value) //WILL cause an issue if players disconnect after selecting a character, but this would only matter if the host started the game before a new player joined/selected a character
            {
                readyToStart.Value = true;
            }
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

    public void CharacterSelected(/*Could put in a gameobject variable to determine which character was selected*/)
    {
        charSelectionUI.SetActive(false);
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
    [Rpc(SendTo.Server)] //Changes the lobby to become a 2v2 game. Will need extra parameters to actually affect the gameplay later
    public void FourPlayerServerRPC()
    {
        playerMaximum.Value = 4;
        lobbySelectionUI.SetActive(false);
        readyToStart.Value = false;

    }
    [Rpc(SendTo.Server)] //keeps track of how many players have selected their character
    public void CharSelectedServerRPC()
    {
        charsSelected.Value++;
    }
}
