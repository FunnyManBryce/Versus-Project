using System.Collections;
using UnityEngine;

public class Knife : Weapon
{
    void Start()
    {
        animator = GetComponent<Animator>();
        weaponRenderer = GetComponent<SpriteRenderer>();
    }
}
