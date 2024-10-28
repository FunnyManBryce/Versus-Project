using System.Collections;
using UnityEngine;

public class Sword : Weapon
{
    void Start()
    {
        animator = GetComponent<Animator>();
        weaponRenderer = GetComponent<SpriteRenderer>();
    }
}
