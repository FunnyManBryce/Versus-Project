using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LameManager : NetworkBehaviour
{
    private NetworkManagerUI networkManagerUI;
    private NetworkManager networkManager;

    [SerializeField] private GameObject lameManager;
    [SerializeField] private GameObject player1SpawnPoint1;
    [SerializeField] private GameObject player2SpawnPoint1;

    public int characterNumber;
    public GameObject[] characterList;

    private bool gameStarted;

    private GameObject Camera;
    private GameObject Character;
    private int Team;
    private ulong clientId;
    private GameObject spawnPoint;

    [SerializeField] private GameObject meleeMinion;
    private GameObject minionSpawnPoint1;
    private GameObject minionSpawnPoint2;
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
        if(minionSpawnTimer < spawnTimerEnd && gameStarted == true)
        {
            minionSpawnTimer += Time.deltaTime;
        } else if(gameStarted == true)
        {
            MinionSpawnServerRPC(Team);
            minionSpawnTimer = 0;
        }
    }

    public void BeginGame()
    {
        StartCoroutine(LoadScene("MapScene"));
    }

    public IEnumerator LoadScene(string sceneName)
    {
        Debug.Log("literally what balls");
        var asyncLoadLevel = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return new WaitUntil(() => asyncLoadLevel.isDone);
        networkManagerUI = FindObjectOfType<NetworkManagerUI>();
        networkManager = FindObjectOfType<NetworkManager>();
        clientId = networkManager.LocalClientId;
        Debug.Log("Client ID: " + clientId + "he he he ha");
        player1SpawnPoint1 = GameObject.Find("Player1SpawnPoint");
        player2SpawnPoint1 = GameObject.Find("Player2SpawnPoint");
        minionSpawnPoint1 = GameObject.Find("minionSpawnPoint1");
        minionSpawnPoint2 = GameObject.Find("minionSpawnPoint2");


        if (clientId == 0)
        {
            Team = 1;
        }
        else
        {
            Team = 2;
        }
        Debug.Log("wth is happening");
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
        if(clientID == 0)
        {
            Camera = character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
            gameStarted = true;
        } else
        {
            CameraOnClientRPC(clientID, team);
        }
        gameStarted = true;
        var characterNetworkObject = character.GetComponent<NetworkObject>();
        characterNetworkObject.SpawnWithOwnership(clientID);
        Debug.Log("They Spawned In!");
    }

    [Rpc(SendTo.NotServer)]
    public void CameraOnClientRPC(ulong clientID, int team)
    {
        gameStarted = true;
        Debug.Log("Huh");
        if (clientId == clientID)
        {
            Camera = Character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
        }
    }

    [Rpc(SendTo.Server)]
    public void MinionSpawnServerRPC(int team)
    {
        if(team == 1)
        {
            var minion = Instantiate(meleeMinion, new Vector3(minionSpawnPoint1.transform.position.x, minionSpawnPoint1.transform.position.y, minionSpawnPoint1.transform.position.z), Quaternion.identity);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
        else
        {
            var minion = Instantiate(meleeMinion, new Vector3(minionSpawnPoint2.transform.position.x, minionSpawnPoint2.transform.position.y, minionSpawnPoint2.transform.position.z), Quaternion.identity);
            var minionNetworkObject = minion.GetComponent<NetworkObject>();
            minionNetworkObject.Spawn();
        }
    }



}