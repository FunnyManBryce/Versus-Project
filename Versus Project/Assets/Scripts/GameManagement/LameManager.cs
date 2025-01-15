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

    public int characterNumber;
    public GameObject[] characterList;
    public GameObject[] teamOneTowers;
    public GameObject[] teamTwoTowers;
    public GameObject[] blueTowerSpawnOrder;
    public GameObject[] redTowerSpawnOrder;
    public GameObject[] teamOneMinionSpawnOrder;
    public GameObject[] teamTwoMinionSpawnOrder;
    public GameObject[] jungleSpawnOrder;
    public Vector3[] MinionSP;
    public Vector3[] JungleSP;
    public Vector3[] redLaneSP;
    public Vector3[] blueLaneSP;
    public Vector3[] playerSP;
    public List<GameObject> teamOneMinions;
    public List<GameObject> teamTwoMinions;

    private bool gameStarted;
    public bool teamOneInhibAlive = true;
    public bool teamTwoInhibAlive = true;

    public GameObject playerOneChar;
    public GameObject playerTwoChar;
    public GameObject Resevoir;
    private GameObject Camera;
    private GameObject Character;
    private int Team;
    public int teamThatWon;
    private ulong clientId;

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
        if (!IsServer) return;
        if (minionSpawnTimer < spawnTimerEnd && gameStarted == true)
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
        if (IsServer)
        {
            LaneSpawnServerRPC();
            JungleSpawnServerRPC();
        }
    }

    [Rpc(SendTo.Server)] 
    public void PlayerSpawnServerRPC(ulong clientID, int team, int charNumber) 
    {
        var character = Instantiate(characterList[charNumber], playerSP[team - 1], Quaternion.identity);
        if (team == 1)
        {
            playerOneChar = character;
        }
        else if(team == 2)
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
        var resevoir = Instantiate(Resevoir, playerSP[0], Quaternion.identity);
        resevoir.GetComponent<Resevoir>().Team = 1;
        resevoir.GetComponent<Resevoir>().teamPlayer = playerOneChar;
        var resevoirNetworkObject = resevoir.GetComponent<NetworkObject>();
        resevoirNetworkObject.Spawn();
        resevoir = Instantiate(Resevoir, playerSP[1], Quaternion.identity);
        resevoir.GetComponent<Resevoir>().Team = 2;
        resevoir.GetComponent<Resevoir>().teamPlayer = playerTwoChar;
        resevoirNetworkObject = resevoir.GetComponent<NetworkObject>();
        resevoirNetworkObject.Spawn();
        for (int i = 0; i < 4; i++)
        {
            var tower = Instantiate(blueTowerSpawnOrder[i], blueLaneSP[i], Quaternion.identity);
            if(i == 1)
            {
                tower.GetComponent<Inhibitor>().Team = 1;
                tower.GetComponent<Inhibitor>().orderInLane = i;
            }
            else
            {
                tower.GetComponent<Tower>().Team = 1;
                tower.GetComponent<Tower>().orderInLane = i;
            }
            teamOneTowers[i] = tower;
            var towerNetworkObject = tower.GetComponent<NetworkObject>();
            towerNetworkObject.Spawn();
        }
        for (int i = 0; i < 4; i++)
        {
            var tower = Instantiate(redTowerSpawnOrder[i], redLaneSP[i], Quaternion.identity);
            if (i == 1)
            {
                tower.GetComponent<Inhibitor>().Team = 2;
                tower.GetComponent<Inhibitor>().orderInLane = i;
            }
            else
            {
                tower.GetComponent<Tower>().Team = 2;
                tower.GetComponent<Tower>().orderInLane = i;
            }
            teamTwoTowers[i] = tower;
            var towerNetworkObject = tower.GetComponent<NetworkObject>();
            towerNetworkObject.Spawn();
        }
    }

    [Rpc(SendTo.Server)]
    public void JungleSpawnServerRPC()
    {
        for (int i = 0; i <= JungleSP.Length - 1; i++)
        {
            var jungle = Instantiate(jungleSpawnOrder[i], JungleSP[i], Quaternion.identity);
            var jungleNetworkObject = jungle.GetComponent<NetworkObject>();
            jungleNetworkObject.Spawn();
        }
    }

    [Rpc(SendTo.Server)]
    public void MinionSpawnServerRPC()
    {
        for (int i = 0; i < 4; i++)
        {
            var minion = Instantiate(teamOneMinionSpawnOrder[i], -MinionSP[i], Quaternion.identity);
            minion.GetComponent<MeleeMinion>().enemyPlayer = playerTwoChar;
            teamOneMinions.Add(minion);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
        for (int i = 0; i < 4; i++)
        {
            var minion = Instantiate(teamTwoMinionSpawnOrder[i], MinionSP[i], Quaternion.identity);
            minion.GetComponent<MeleeMinion>().enemyPlayer = playerOneChar;
            teamTwoMinions.Add(minion);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
        if (teamTwoInhibAlive == false)
        {
            var superMinion = Instantiate(teamOneMinionSpawnOrder[4], -MinionSP[4], Quaternion.identity);
            superMinion.GetComponent<MeleeMinion>().enemyPlayer = playerTwoChar;
            teamOneMinions.Add(superMinion);
            var SMinionNetworkObject = superMinion.GetComponent<NetworkObject>();
            SMinionNetworkObject.Spawn();
        }
        if (teamOneInhibAlive == false)
        {
            var superMinion = Instantiate(teamTwoMinionSpawnOrder[4], MinionSP[4], Quaternion.identity);
            superMinion.GetComponent<MeleeMinion>().enemyPlayer = playerOneChar;
            teamTwoMinions.Add(superMinion);
            var SMinionNetworkObject = superMinion.GetComponent<NetworkObject>();
            SMinionNetworkObject.Spawn();
        }
    }

    [Rpc(SendTo.NotServer)]
    public void TeamTwoWinClientRPC()
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
            if(teamOneTowersLeft.Value < 0)
            {
                gameStarted = false;
                teamThatWon = 2;
                TeamTwoWinClientRPC();
                SceneManager.LoadScene("GameOver");
            }
        } else if(team == 2)
        {
            teamTwoTowersLeft.Value--;
            if (teamTwoTowersLeft.Value < 0)
            {
                gameStarted = false;
                teamThatWon = 1;
                TeamOneWinClientRPC();
                SceneManager.LoadScene("GameOver");
            }
        }
    }

}
