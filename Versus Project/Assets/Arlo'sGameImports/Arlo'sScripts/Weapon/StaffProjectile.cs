using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffProjectile : MonoBehaviour
{
    public GameObject projectile;

    public float travelSpeed;
    public float lifespan;

    public Vector2 projectileTrajectory = new Vector2(1, 0);

    public int damage;
    // Start is called before the first frame update
    void Start()
    {
        Staff staff = GameObject.Find("Staff").GetComponent<Staff>();
        damage = staff.projectileDamage;
        transform.localScale += new Vector3(staff.scaleAmount, staff.scaleAmount, staff.scaleAmount);
    }

    // Update is called once per frame
    void Update()
    {
        lifespan -= Time.deltaTime;
        if (lifespan <= 0)
        {
            Destroy(projectile);
        }
        projectileTrajectory = new Vector2(1, 0);
        transform.Translate(projectileTrajectory * Time.deltaTime * travelSpeed);
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.layer == 8)
        {
            Destroy(projectile);
            Health health;
            health = col.gameObject.GetComponent<Health>();
            health.GetHit(damage, projectile);
            
        }

    }
}
