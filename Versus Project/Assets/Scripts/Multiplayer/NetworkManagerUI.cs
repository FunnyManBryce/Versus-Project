using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private LameManager lameManager;
    [SerializeField] private NetworkManager networkManagerScript;
    public GameObject playersInLobby;
    public TMP_Text playersInLobbyText;
    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerMaximum = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playersReady = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> readyToStart = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool serverQuitting = false;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button startButton;

    [SerializeField] private GameObject lobbyCreationUI;
    [SerializeField] private GameObject lobbySelectionUI;
    [SerializeField] private GameObject charSelectionUI;
    [SerializeField] private GameObject readyToStartUI;
    [SerializeField] private GameObject QuitOption;
    [SerializeField] private GameObject IPText;
    public GameObject Character;
    public int characterNumber;
    public GameObject networkManager;
    public GameObject networkManagerUI;



    private void Awake()
    {
        hostButton.onClick.AddListener(() => { //Host button creates a lobby
            NetworkManager.Singleton.StartHost();
            QuitOption.SetActive(true);
            //lobbySelectionUI.SetActive(true); //For now let's only have a 1v1 mode. The code is in place to change this though
        });
        clientButton.onClick.AddListener(() => { //Lobby button joins a lobby
            NetworkManager.Singleton.StartClient();
            QuitOption.SetActive(true);
        });
        startButton.onClick.AddListener(() => //starts game
        {
            if (IsServer == true /*&& readyToStart.Value == true*/ ) //REMOVE THIS BEFORE GAME IS UPLOADED
            {
                SceneManager.LoadScene("MapScene");
                StartGameClientRPC();
                lameManager.BeginGame();
            }
        });
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("NetworkSpawned");
        if(IsServer)
        {
            playersReady.Value = 0;
            playerMaximum.Value = 2;
            totalPlayers.Value = 0;
        }
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
        playersReady.OnValueChanged += (int previousValue, int newValue) => //if everyone has ready'd up, the game can start
        {
            if (playersReady.Value == 0)
            {
                charSelectionUI.SetActive(true);
                readyToStartUI.SetActive(false);
            }
            if (playersReady.Value == playerMaximum.Value && IsServer == true) 
            {
                readyToStart.Value = true; 
            } 
            else if (playersReady.Value != playerMaximum.Value && IsServer == true)
            {
                readyToStart.Value = false; 
            }
        };
        PlayerConnectedServerRPC();
        if (totalPlayers.Value >= playerMaximum.Value) //if there are too many players in the lobby, kick out the most recently joined one
        {
            DisconnectClientServerRPC(networkManagerScript.LocalClientId);
        }
    }

    private void Start() 
    {
        DontDestroyOnLoad(networkManagerUI);
        networkManagerScript.OnClientDisconnectCallback += PlayerDisconnectedServerRPC; //Makes it so PlayerDisconnectedServerRPC triggers whenever a client disconnects
    }
 
    public void IPString(string s) //Sets IP to whatever is typed into the IP box
    {
        networkManager.GetComponent<UnityTransport>().ConnectionData.Address = s; 
        Debug.Log(networkManager.GetComponent<UnityTransport>().ConnectionData.Address);
    }

    public void CharacterSelected(GameObject character) //Allows us to make each button choose a seperate characters! Needs a way to determine if people pick the same one though...
    {
        Character = character;
    }

    public void CharacterNumber(int charNumber)
    {
        characterNumber = charNumber;
    }

    public void ReadyUp() 
    {
        if(Character != null) //Makes sure the player has selected a character before they're counted as ready
        {
            ReadyUpServerRPC();
            charSelectionUI.SetActive(false);
            readyToStartUI.SetActive(true);
        }
    }

    public void UnReadyUp()
    {
        charSelectionUI.SetActive(true);
        readyToStartUI.SetActive(false);
        UnReadyUpServerRPC();
    }

    public void Quit() 
    {
        if(IsServer == true) //Server quit shuts down entire lobby
        {
            networkManagerScript.Shutdown();
            QuitOption.SetActive(false);
            charSelectionUI.SetActive(false);
            playersInLobby.SetActive(false);
            readyToStartUI.SetActive(false);
            lobbySelectionUI.SetActive(false);
            lobbyCreationUI.SetActive(true);
            Character = null;
            Debug.Log("Server Disconnected");
            serverQuitting = true;
            ServerQuitClientRPC();
        }
        else //Client only quits individual client
        {
            DisconnectClientServerRPC(networkManagerScript.LocalClientId);
            QuitOption.SetActive(false);
            charSelectionUI.SetActive(false);
            playersInLobby.SetActive(false);
            readyToStartUI.SetActive(false);
            lobbySelectionUI.SetActive(false);
            lobbyCreationUI.SetActive(true);
            Debug.Log("Client Disconnected");
            Character = null;
        }
    }

    [Rpc(SendTo.Server)] //sends info that a client has connected to the server. This is then syncronized accross all the clients, since that is how network variables work
    public void PlayerConnectedServerRPC()
    {
        totalPlayers.Value++;
    }
    [Rpc(SendTo.Server)] //sends info that a client has disconnected to the server. This is then syncronized accross all the clients, since that is how network variables work
    public void PlayerDisconnectedServerRPC(ulong clientID)
    {
        if(serverQuitting == false)
        {
            totalPlayers.Value--;
            playersReady.Value = 0;
        }
        else
        {
            serverQuitting = false;
        }
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
        playersReady.Value = 0;
    }
    [Rpc(SendTo.Server)] 
    public void ReadyUpServerRPC()
    {
        playersReady.Value++;
    }
    [Rpc(SendTo.Server)]
    public void UnReadyUpServerRPC()
    {
        playersReady.Value--;
    }
    [Rpc(SendTo.NotServer)] 
    public void ServerQuitClientRPC()
    {
        QuitOption.SetActive(false);
        charSelectionUI.SetActive(false);
        playersInLobby.SetActive(false);
        readyToStartUI.SetActive(false);
        lobbySelectionUI.SetActive(false);
        lobbyCreationUI.SetActive(true);
        Character = null;
        Debug.Log("Server Disconnected");
    }
    [Rpc(SendTo.NotServer)]
    public void StartGameClientRPC()
    {
        SceneManager.LoadScene("MapScene");
        lameManager.BeginGame();

    }
}
