using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staff : Weapon
{
    public GameObject projectile;
    public GameObject weaponParent;
    public int projectileDamage;
    public float scaleAmount = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        weaponRenderer = GetComponent<SpriteRenderer>();
    }

    public void StaffBlast()
    {
        Instantiate(projectile, new Vector3(attackOrigin.position.x, attackOrigin.position.y), weaponParent.transform.rotation);
    }
}
