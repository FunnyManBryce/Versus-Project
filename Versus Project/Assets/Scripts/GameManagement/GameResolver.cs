using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResolver : MonoBehaviour
{
    public GameObject TeamOneWon;
    public GameObject TeamTwoWoo;
    public LameManager lameManager;

    void Start()
    {
        lameManager = FindObjectOfType<LameManager>();
        if(lameManager.teamThatWon == 1)
        {
            TeamOneWon.SetActive(true);
        } else
        {
            TeamTwoWoo.SetActive(true);
        }
    }

   
}
