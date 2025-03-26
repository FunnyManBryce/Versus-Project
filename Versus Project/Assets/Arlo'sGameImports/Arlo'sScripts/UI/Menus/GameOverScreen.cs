using UnityEngine.SceneManagement;

public class GameOverScreen : RestartScene
{
    public void Setup()
    {
        gameObject.SetActive(true);
    }

    public void RestartButton()
    {
        SceneManager.LoadScene(restartScene);
    }
}
