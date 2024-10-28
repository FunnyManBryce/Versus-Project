using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveCounter : MonoBehaviour
{
    public ArenaManager arenaManager;
    public TMP_Text WaveText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        WaveText.text = "Wave:" + arenaManager.realWaveNumber + "/15";
    }
}
