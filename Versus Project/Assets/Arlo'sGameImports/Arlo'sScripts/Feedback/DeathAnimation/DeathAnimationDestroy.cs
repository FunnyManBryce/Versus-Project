using UnityEngine;

public class DeathAnimationDestroy : MonoBehaviour
{
    public float timeToDestroy = 5f;

    void Start()
    {
        Invoke("DestroyObject", timeToDestroy);
    }

    void DestroyObject()
    {
        Destroy(gameObject);
    }
}