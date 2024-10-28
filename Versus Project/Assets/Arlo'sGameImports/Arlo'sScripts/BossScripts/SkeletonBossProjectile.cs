using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonBossProjectile : MonoBehaviour
{
    public GameObject projectile;

    public float travelSpeed;
    public float lifespan = 3f;

    public Vector2 projectileTrajectory = new Vector2(1, 0);

    public int damage = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifespan -= Time.deltaTime;
        if(lifespan <= 0 )
        {
            Destroy(projectile);
        }
        projectileTrajectory = new Vector2(1, 0);
        transform.Translate(projectileTrajectory * Time.deltaTime * travelSpeed);
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "Player")
        {
            Health health;
            health = col.gameObject.GetComponent<Health>();
            health.GetHit(damage, projectile);
            Destroy(projectile);
        }
        
    }
}
