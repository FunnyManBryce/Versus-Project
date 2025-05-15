using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LameManager : NetworkBehaviour
{
    private NetworkManagerUI networkManagerUI;
    private NetworkManager networkManager;

    public NetworkVariable<int> teamOneTowersLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> teamTwoTowersLeft = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> matchTimer = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> intMatchTimer = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> respawnLength = new NetworkVariable<float>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsOneVOne = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float currentRespawnTimer;
    public bool isRespawning;

    public float oneVOneTime = 600f;

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
            Camera = character.transform.Find("Main Camera").gameObject; ;
            Camera.SetActive(false);
        }
    }

    void Update()
    {
        if (isRespawning)
        {
            currentRespawnTimer -= Time.deltaTime;
        } else
        {
            currentRespawnTimer = respawnLength.Value;
        }
        if (!IsServer) return;
        if(matchTimer.Value >= oneVOneTime && IsOneVOne.Value == false)
        {
            foreach (var tower in teamOneTowers)
            {
                if (tower != null)
                {
                    tower.GetComponent<Tower>().inSuddenDeath = true;
                    tower.GetComponent<Health>().invulnerable = true;
                }
            }
            foreach (var tower in teamTwoTowers)
            {
                if (tower != null)
                {
                    tower.GetComponent<Tower>().inSuddenDeath = true;
                    tower.GetComponent<Health>().invulnerable = true;
                }
            }
            IsOneVOne.Value = true;
            playerOneChar.GetComponent<BasePlayerController>().SuddenDeath.Value = true;
            playerTwoChar.GetComponent<BasePlayerController>().SuddenDeath.Value = true;
        }
        if (gameStarted)
        {
            matchTimer.Value += Time.deltaTime;
            intMatchTimer.Value = (int)matchTimer.Value;
            respawnLength.Value = 2 + (matchTimer.Value * 0.03f);
            if (minionSpawnTimer < spawnTimerEnd)
            {
                minionSpawnTimer += Time.deltaTime;
            }
            else
            {
                MinionSpawnServerRPC();
                minionSpawnTimer = 0;
            }
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

    public IEnumerator PlayerDeath(NetworkObject player, float respawnTimer, ulong clientID)
    {
        
        isRespawning = true;
        currentRespawnTimer = respawnTimer;
        if (IsOneVOne.Value)
        {
            if(clientID == 0)
            {
                TeamTwoWinServerRPC();
            } else
            {
                TeamOneWinServerRPC();
            }
            yield break;
        }
        yield return new WaitForSeconds(respawnTimer);
        isRespawning = false;
        if (IsOneVOne.Value)
        {
            yield break;
        }
        else
        {
            player.transform.position = playerSP[player.GetComponent<BasePlayerController>().teamNumber.Value - 1];
            player.GetComponent<BasePlayerController>().isDead.Value = false;
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
        else if (team == 2)
        {
            playerTwoChar = character;
        }
        character.GetComponent<BasePlayerController>().clientID = clientID;
        var characterNetworkObject = character.GetComponent<NetworkObject>();
        characterNetworkObject.SpawnWithOwnership(clientID);
        if (clientID == 0)
        {
            Camera = character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
            gameStarted = true;
            characterNetworkObject.AddComponent<AudioListener>();
        }
        else
        {
            CameraOnClientRPC(clientID, team, characterNetworkObject);
        }

    }

    [Rpc(SendTo.NotServer)]
    public void CameraOnClientRPC(ulong clientID, int team, NetworkObjectReference character)
    {
        Debug.Log("???");
        if (clientId == clientID)
        {
            Debug.Log("????????");
            if (character.TryGet(out NetworkObject c))
            {
                c.AddComponent<AudioListener>();
                Camera = c.transform.Find("Main Camera").gameObject;
                Camera.SetActive(true);
            }
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
            if (i == 1)
            {
                tower.GetComponent<Inhibitor>().Team = 1;
                tower.GetComponent<Inhibitor>().health.Team.Value = 1;
                tower.GetComponent<Inhibitor>().orderInLane = i;
            }
            else
            {
                tower.GetComponent<Tower>().Team = 1;
                tower.GetComponent<Tower>().health.Team.Value = 1;
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
                tower.GetComponent<Inhibitor>().health.Team.Value = 2;
                tower.GetComponent<Inhibitor>().orderInLane = i;
            }
            else
            {
                tower.GetComponent<Tower>().Team = 2;
                tower.GetComponent<Tower>().health.Team.Value = 2;
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
            minion.GetComponent<MeleeMinion>().health.Team.Value = 1;
            teamOneMinions.Add(minion);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
        for (int i = 0; i < 4; i++)
        {
            var minion = Instantiate(teamTwoMinionSpawnOrder[i], MinionSP[i], Quaternion.identity);
            minion.GetComponent<MeleeMinion>().enemyPlayer = playerOneChar;
            minion.GetComponent<MeleeMinion>().health.Team.Value = 2;
            teamTwoMinions.Add(minion);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
        if (teamTwoInhibAlive == false)
        {
            var superMinion = Instantiate(teamOneMinionSpawnOrder[4], -MinionSP[4], Quaternion.identity);
            superMinion.GetComponent<MeleeMinion>().enemyPlayer = playerTwoChar;
            superMinion.GetComponent<MeleeMinion>().health.Team.Value = 1;
            teamOneMinions.Add(superMinion);
            var SMinionNetworkObject = superMinion.GetComponent<NetworkObject>();
            SMinionNetworkObject.Spawn();
        }
        if (teamOneInhibAlive == false)
        {
            var superMinion = Instantiate(teamTwoMinionSpawnOrder[4], MinionSP[4], Quaternion.identity);
            superMinion.GetComponent<MeleeMinion>().enemyPlayer = playerOneChar;
            superMinion.GetComponent<MeleeMinion>().health.Team.Value = 2;
            teamTwoMinions.Add(superMinion);
            var SMinionNetworkObject = superMinion.GetComponent<NetworkObject>();
            SMinionNetworkObject.Spawn();
        }
    }


    [Rpc(SendTo.Server)]
    public void TeamTwoWinServerRPC()
    {
        teamThatWon = 2;
        gameStarted = false;
        TeamTwoWinClientRPC();
        SceneManager.LoadScene("GameOver");
    }

    [Rpc(SendTo.NotServer)]
    public void TeamTwoWinClientRPC()
    {
        teamThatWon = 2;
        gameStarted = false;
        SceneManager.LoadScene("GameOver");
    }

    [Rpc(SendTo.Server)]
    public void TeamOneWinServerRPC()
    {
        teamThatWon = 1;
        gameStarted = false;
        TeamOneWinClientRPC();
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
        if (team == 1)
        {
            teamOneTowersLeft.Value--;
            if (teamOneTowersLeft.Value < 0)
            {
                gameStarted = false;
                teamThatWon = 2;
                TeamTwoWinClientRPC();
                SceneManager.LoadScene("GameOver");
            }
        }
        else if (team == 2)
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
