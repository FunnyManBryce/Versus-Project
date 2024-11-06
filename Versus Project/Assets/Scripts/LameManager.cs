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

    private GameObject Camera;
    private GameObject Character;
    private int Team;
    private ulong clientId;
    private GameObject spawnPoint;
    private bool playerSpawned;

    private void Start()
    {
        DontDestroyOnLoad(lameManager);
        foreach(var character in characterList)
        {
            Camera = character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(false);
        }
    }

    void Update()
    {
        
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
        } else
        {
            CameraOnClientRPC(clientID, team);
        }
        
        var characterNetworkObject = character.GetComponent<NetworkObject>();
        characterNetworkObject.SpawnWithOwnership(clientID);
        Debug.Log("They Spawned In!");
    }

    [Rpc(SendTo.NotServer)]
    public void CameraOnClientRPC(ulong clientID, int team)
    {
        Debug.Log("Huh");
        if (clientId == clientID)
        {
            Camera = Character.transform.Find("Main Camera").gameObject;
            Camera.SetActive(true);
        }
    }



}
