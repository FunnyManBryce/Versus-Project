using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaBossProjectileTracking : MonoBehaviour
{
    public Transform target;
    public Vector2 playerPosition;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        target = player.transform;
    }

    // Update is called once per frame
    void Update()
    {
        playerPosition = new Vector2(target.position.x, target.position.y);

        Vector2 direction = ((playerPosition - (Vector2)transform.position).normalized);

        transform.right = direction;

        Vector2 scale = transform.localScale;

        transform.localScale = scale;
    }
}
