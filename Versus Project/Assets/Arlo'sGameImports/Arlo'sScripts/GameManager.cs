using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameOverScreen gameOverScreen;
    public Health health; 

    public void Update()
    {
        if (gameOverScreen != null)
        {
            if (health == null)
            {
                gameOverScreen.Setup();
            }
        }
    }
}
