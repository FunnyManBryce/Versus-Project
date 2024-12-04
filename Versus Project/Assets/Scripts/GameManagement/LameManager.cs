using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LameManager : NetworkBehaviour
{
    private NetworkManagerUI networkManagerUI;
    private NetworkManager networkManager;

    public NetworkVariable<int> teamOneTowersLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> teamTwoTowersLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private GameObject lameManager;
    [SerializeField] private GameObject player1SpawnPoint1;
    [SerializeField] private GameObject player2SpawnPoint1;

    public int characterNumber;
    public GameObject[] characterList;
    public GameObject[] teamOneTowers;
    public GameObject[] teamTwoTowers;
    public List<GameObject> teamOneMinions;
    public List<GameObject> teamTwoMinions;

    private bool gameStarted;

    public GameObject playerOneChar;
    public GameObject playerTwoChar;
    private GameObject Camera;
    private GameObject Character;
    private int Team;
    public int teamThatWon;
    private ulong clientId;
    private GameObject spawnPoint;

    [SerializeField] private GameObject teamOneMeleeMinion;
    [SerializeField] private GameObject teamTwoMeleeMinion;
    [SerializeField] private GameObject Tower;
    public GameObject minionSpawnPoint1;
    public GameObject minionSpawnPoint2;
    private float minionSpawnTimer;
    [SerializeField] private float spawnTimerEnd;

    private void Start()
    {
        DontDestroyOnLoad(lameManager);
        gameStarted = false;
        foreach (var character in characterList)
        {
            Camera = character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(false);
        }
    }

    void Update()
    {
        if(gameStarted == true && Input.GetKeyDown(KeyCode.Tab))
        {
            gameStarted = false;
            if (Team == 1)
            {
                teamThatWon = 1;
                TeamOneWinClientRPC();
                SceneManager.LoadScene("GameOver");
            }
            else
            {
                SceneManager.LoadScene("GameOver");
                teamThatWon = 2;
                TeamTwoWinServerRPC();
            }
        }
        if (!IsServer) return;
        if(minionSpawnTimer < spawnTimerEnd && gameStarted == true)
        {
            minionSpawnTimer += Time.deltaTime;
        } else if(gameStarted == true)
        {
            MinionSpawnServerRPC();
            minionSpawnTimer = 0;
        }
    }

    public void BeginGame()
    {
        StartCoroutine(LoadScene("MapScene"));
        teamOneTowersLeft.Value = 3;
        teamTwoTowersLeft.Value = 3;
    }

    public IEnumerator LoadScene(string sceneName)
    {
        var asyncLoadLevel = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return new WaitUntil(() => asyncLoadLevel.isDone);
        networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        networkManager = FindObjectOfType<NetworkManager>();
        clientId = networkManager.LocalClientId;
        Debug.Log("Client ID: " + clientId + "he he he ha");
        player1SpawnPoint1 = GameObject.Find("Player1SpawnPoint");
        player2SpawnPoint1 = GameObject.Find("Player2SpawnPoint");
        minionSpawnPoint1 = GameObject.Find("MinionSpawnPoint1");
        minionSpawnPoint2 = GameObject.Find("MinionSpawnPoint2");

        if(IsServer)
        {
            LaneSpawnServerRPC();
        }
        teamOneTowers[0] = GameObject.Find("Player1Pentagon");
        teamOneTowers[1] = GameObject.Find("Player1Inhibitor");

        teamTwoTowers[0] = GameObject.Find("Player2Pentagon");
        teamTwoTowers[1] = GameObject.Find("Player2Inhibitor");
          
        if (clientId == 0)
        {
            Team = 1;
        }
        else
        {
            Team = 2;
        }
        gameStarted = true;
        Character = networkManagerUI.Character;
        characterNumber = networkManagerUI.characterNumber;
        PlayerSpawnServerRPC(clientId, Team, characterNumber);
    }

    [Rpc(SendTo.Server)] 
    public void PlayerSpawnServerRPC(ulong clientID, int team, int charNumber) 
    {
        if (team == 1)
        {
            spawnPoint = player1SpawnPoint1;
        }
        else if (team == 2)
        {
            spawnPoint = player2SpawnPoint1;
        }
        var character = Instantiate(characterList[charNumber], new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, spawnPoint.transform.position.z), Quaternion.identity);
        if(team == 1)
        {
            playerOneChar = character;
        }
        else
        {
            playerTwoChar = character;
        }
        if (clientID == 0)
        {
            Camera = character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
            gameStarted = true;
        } else
        {
            CameraOnClientRPC(clientID, team);
        }
        var characterNetworkObject = character.GetComponent<NetworkObject>();
        characterNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.NotServer)]
    public void CameraOnClientRPC(ulong clientID, int team)
    {
        if (clientId == clientID)
        {
            Camera = Character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
        }
    }

    [Rpc(SendTo.Server)]
    public void LaneSpawnServerRPC()
    {
        Debug.Log("how many times is this happening?!?!?!");
        var tower = Instantiate(Tower, new Vector3(-20, 0, 0), Quaternion.identity);
        tower.GetComponent<Tower>().Team = 1;
        teamOneTowers[3] = tower;
        var towerNetworkObject = tower.GetComponent<NetworkObject>();
        towerNetworkObject.Spawn();

        tower = Instantiate(Tower, new Vector3(-50, 0, 0), Quaternion.identity);
        tower.GetComponent<Tower>().Team = 1;
        teamOneTowers[2] = tower;
        towerNetworkObject = tower.GetComponent<NetworkObject>();
        towerNetworkObject.Spawn();

        tower = Instantiate(Tower, new Vector3(20, 0, 0), Quaternion.identity);
        tower.GetComponent<Tower>().Team = 2;
        teamTwoTowers[3] = tower;
        towerNetworkObject = tower.GetComponent<NetworkObject>();
        towerNetworkObject.Spawn();

        tower = Instantiate(Tower, new Vector3(50, 0, 0), Quaternion.identity);
        tower.GetComponent<Tower>().Team = 2;
        teamTwoTowers[2] = tower;
        towerNetworkObject = tower.GetComponent<NetworkObject>();
        towerNetworkObject.Spawn();
    }

    [Rpc(SendTo.Server)]
    public void MinionSpawnServerRPC()
    {
        var minion = Instantiate(teamOneMeleeMinion, new Vector3(minionSpawnPoint1.transform.position.x, minionSpawnPoint1.transform.position.y, minionSpawnPoint1.transform.position.z), Quaternion.identity);
        minion.GetComponent<MeleeMinion>().Team = 1;
        minion.GetComponent<MeleeMinion>().enemyPlayer = playerTwoChar;
        teamOneMinions.Add(minion);
        var minionNetworkObject = minion.GetComponent<NetworkObject>();
        minionNetworkObject.Spawn();

        minion = Instantiate(teamTwoMeleeMinion, new Vector3(minionSpawnPoint2.transform.position.x, minionSpawnPoint2.transform.position.y, minionSpawnPoint2.transform.position.z), Quaternion.identity);
        minion.GetComponent<MeleeMinion>().Team = 2;
        minion.GetComponent<MeleeMinion>().enemyPlayer = playerOneChar;
        teamTwoMinions.Add(minion);
        minionNetworkObject = minion.GetComponent<NetworkObject>();
        minionNetworkObject.Spawn();
    }

    [Rpc(SendTo.Server)]
    public void TeamTwoWinServerRPC()
    {
        teamThatWon = 2;
        gameStarted = false;
        SceneManager.LoadScene("GameOver");
    }

    [Rpc(SendTo.NotServer)]
    public void TeamOneWinClientRPC()
    {
        teamThatWon = 1;
        gameStarted = false;
        SceneManager.LoadScene("GameOver");
    }
    
    [Rpc(SendTo.Server)]
    public void TowerDestroyedServerRPC(int team)
    {
        if(team == 1)
        {
            teamOneTowersLeft.Value--;
        } else if(team == 2)
        {
            teamTwoTowersLeft.Value--;
        }
    }

}
