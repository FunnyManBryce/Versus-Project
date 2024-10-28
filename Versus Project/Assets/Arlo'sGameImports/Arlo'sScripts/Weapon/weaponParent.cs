using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class weaponParent : MonoBehaviour
{

    public SpriteRenderer characterRenderer;
    public Vector2 pointerPosition { get; set; }

    public bool IsAttacking { get; private set; }

    public List<Weapon> weapons = new List<Weapon>();
    private int activeWeaponIndex = 0;

    public void ResetIsAttacking()
    {
        IsAttacking = false;
    }

    private void Update()
    {
        if (IsAttacking)
            return;

        Vector2 direction = ((pointerPosition - (Vector2)transform.position).normalized);

        transform.right = direction;

        Vector2 scale = transform.localScale;

        if(direction.x < 0)
        {
            scale.y = -1;
            characterRenderer.flipX = true;

        }
        else if(direction.x > 0) 
        {
            scale.y = 1;
            characterRenderer.flipX = false;
        }
        transform.localScale = scale;

        foreach (Weapon weapon in weapons)
        {
            if (transform.eulerAngles.z > 0 && transform.eulerAngles.z < 180)
            {
                weapon.weaponRenderer.sortingOrder = characterRenderer.sortingOrder - 1;
            }
            else
            {
                weapon.weaponRenderer.sortingOrder = characterRenderer.sortingOrder + 1;
            }
        }
        for (int i = 1; i <= weapons.Count; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                SwitchToWeapon(i - 1);
            }
        }
    }

    public void Attack()
    {
        if (weapons.Count == 0)
            return;

        Weapon activeWeapon = weapons[activeWeaponIndex];

        if (activeWeapon.attackBlocked)
            return;

        activeWeapon.animator.SetTrigger("Attack");
        IsAttacking = true;
        activeWeapon.attackBlocked = true;
        StartCoroutine(activeWeapon.DelayAttack());

        activeWeapon.DetectColliders();
    }


    private IEnumerator DelayAttack()
    {
        foreach (Weapon weapon in weapons)
        {
            yield return new WaitForSeconds(weapon.attackDelay);
            weapon.attackBlocked = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        foreach (Weapon weapon in weapons)
        {
            Gizmos.color = Color.blue;
            Vector3 position = weapon.attackOrigin == null ? Vector3.zero : weapon.attackOrigin.position;
            Gizmos.DrawWireSphere(position, weapon.radius);
        }
    }
    public void SwitchToWeapon(int index)
    {
        if (weapons.Count == 0)
            return;

        weapons[activeWeaponIndex].gameObject.SetActive(false);

        activeWeaponIndex = Mathf.Clamp(index, 0, weapons.Count - 1);

        weapons[activeWeaponIndex].gameObject.SetActive(true);
    }
}