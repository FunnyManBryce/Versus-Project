using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResolver : MonoBehaviour
{
    public GameObject TeamOneWon;
    public GameObject TeamTwoWoo;
    public LameManager lameManager;

    void Start()
    {
        lameManager = FindObjectOfType<LameManager>();
        if (lameManager.teamThatWon == 1)
        {
            TeamOneWon.SetActive(true);
        }
        else
        {
            TeamTwoWoo.SetActive(true);

        }
    }

    public void RestartGame()
    {
        LameManager lameManager = FindFirstObjectByType<LameManager>();
        Destroy(lameManager.gameObject);

        BryceAudioManager bam = FindFirstObjectByType<BryceAudioManager>();
        Destroy(bam.gameObject);

        NetworkManagerUI netManager = FindFirstObjectByType<NetworkManagerUI>();
        Destroy(netManager.gameObject);

        NetworkManager netManagerNotUI = FindFirstObjectByType<NetworkManager>();
        Destroy(netManagerNotUI.gameObject);

        SceneManager.LoadScene("MainMenu");

    }


}
