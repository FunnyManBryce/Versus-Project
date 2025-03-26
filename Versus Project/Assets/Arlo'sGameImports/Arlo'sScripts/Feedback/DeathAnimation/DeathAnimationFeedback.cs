using UnityEngine;

public class DeathAnimationFeedback : MonoBehaviour
{
    public GameObject gameObjectToSpawn;
    public void PlayFeedback()
    {
        Instantiate(gameObjectToSpawn, transform.position, Quaternion.identity);
    }
}
