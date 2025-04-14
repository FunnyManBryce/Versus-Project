using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManagerUI : NetworkBehaviour
{
    public BryceAudioManager bAM;
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
    [SerializeField] private GameObject[] characterInfoUI;
    [SerializeField] private GameObject QuitOption;
    [SerializeField] private GameObject IPText;
    public GameObject Character;
    public int characterNumber;
    public GameObject networkManager;
    public GameObject networkManagerUI;



    private void Awake()
    {
        bAM.Play("Main Menu Theme", gameObject.transform.position);
        hostButton.onClick.AddListener(() =>
        { //Host button creates a lobby
            bAM.Play("Button Press", gameObject.transform.position);
            NetworkManager.Singleton.StartHost();
            QuitOption.SetActive(true);
            //lobbySelectionUI.SetActive(true); //For now let's only have a 1v1 mode. The code is in place to change this though
        });
        clientButton.onClick.AddListener(() =>
        { //Lobby button joins a lobby
            bAM.Play("Button Press", gameObject.transform.position);
            NetworkManager.Singleton.StartClient();
            QuitOption.SetActive(true);
        });
        startButton.onClick.AddListener(() => //starts game
        {
            bAM.StopAllSounds();
            if (IsServer == true /*&& readyToStart.Value == true*/ ) //REMOVE THIS BEFORE GAME IS UPLOADED
            {
                bAM.StopAllSounds();
                SceneManager.LoadScene("MapScene");
                StartGameClientRPC();
                lameManager.BeginGame();
            }
        });
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("NetworkSpawned");
        if (IsServer)
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
            QuitOption.SetActive(false);
            charSelectionUI.SetActive(false);
            playersInLobby.SetActive(false);
            readyToStartUI.SetActive(false);
            lobbySelectionUI.SetActive(false);
            lobbyCreationUI.SetActive(true);
            Character = null;
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
        if (charNumber == characterNumber) return;
        bAM.Play("Button Press", gameObject.transform.position);
        characterNumber = charNumber;
        if(characterNumber == 1) //Decay sound
        {
            bAM.Play("Decay Pick", gameObject.transform.position);
            bAM.Stop("Puppeteering Pick");
        }
        if (characterNumber == 2) //Puppeteering sound
        {
            bAM.Play("Puppeteering Pick", gameObject.transform.position);
            bAM.Stop("Decay Pick");
        }
        foreach (GameObject character in characterInfoUI)
        {
            character.SetActive(false);
        }
        characterInfoUI[charNumber].SetActive(true);
    }

    public void ReadyUp()
    {
        if (Character != null) //Makes sure the player has selected a character before they're counted as ready
        {
            bAM.Play("Button Press", gameObject.transform.position);
            ReadyUpServerRPC();
            charSelectionUI.SetActive(false);
            readyToStartUI.SetActive(true);
        }
    }

    public void UnReadyUp()
    {
        bAM.Play("Button Press", gameObject.transform.position);
        charSelectionUI.SetActive(true);
        readyToStartUI.SetActive(false);
        foreach (GameObject character in characterInfoUI)
        {
            character.SetActive(false);
        }
        UnReadyUpServerRPC();
    }

    public void Quit()
    {
        bAM.Play("Button Press", gameObject.transform.position);
        foreach (GameObject character in characterInfoUI)
        {
            character.SetActive(false);
        }
        if (IsServer == true) //Server quit shuts down entire lobby
        {
            networkManagerScript.Shutdown();
            QuitOption.SetActive(false);
            charSelectionUI.SetActive(false);
            playersInLobby.SetActive(false);
            readyToStartUI.SetActive(false);
            lobbySelectionUI.SetActive(false);
            lobbyCreationUI.SetActive(true);
            Character = null;
            serverQuitting = true;
            ServerQuitClientRPC();
        }
        else //Client only quits individual client
        {
            QuitOption.SetActive(false);
            charSelectionUI.SetActive(false);
            playersInLobby.SetActive(false);
            readyToStartUI.SetActive(false);
            lobbySelectionUI.SetActive(false);
            lobbyCreationUI.SetActive(true);
            Character = null;
            DisconnectClientServerRPC(networkManagerScript.LocalClientId);
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
        if (serverQuitting == false)
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
        bAM.StopAllSounds();
        SceneManager.LoadScene("MapScene");
        lameManager.BeginGame();

    }
}
