using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shockwave : MonoBehaviour
{
    //public LavaGolemAttack lavaGolemAttack;
    public Enemy enemyScript;
    public GameObject shockwave;
    public int shockwaveDamage = 30;
    public Vector2 Trajectory;
    public float travelSpeed;
    public float lifespan = 3f;

    void Start()
    {

        Trajectory = new Vector2(1, 0);
    }

    void Update()
    {
        lifespan -= Time.deltaTime;
        transform.Translate(Trajectory * Time.deltaTime * travelSpeed);
        if (lifespan <= 0)
        {
            Destroy(shockwave);
        }
    }

    
    private void OnCollisionStay2D(Collision2D col)
    {

        if (col.gameObject.tag == "Player")
        {
            Health health;
            health = col.gameObject.GetComponent<Health>();
            health.GetHit(shockwaveDamage, shockwave);
            Destroy(shockwave);

        }
    }
}
