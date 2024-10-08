using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : MonoBehaviour
{
    public GameObject playersInLobby;
    public TMP_Text playersInLobbyText;
    public static int totalPlayers = 0;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    public GameObject networkManager;
    
    private void Awake()
    {
       
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            playersInLobby.SetActive(true);
            totalPlayers++;
            playersInLobbyText.text = "Players in Lobby: " + totalPlayers;
        });
        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
    }

    public void IPString(string s)
    {
        networkManager.GetComponent<UnityTransport>().ConnectionData.Address = s;
        Debug.Log(networkManager.GetComponent<UnityTransport>().ConnectionData.Address);
    }


}
