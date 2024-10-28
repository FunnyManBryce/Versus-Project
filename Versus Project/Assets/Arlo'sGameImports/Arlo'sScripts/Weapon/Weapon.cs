using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float detectionDelay = 0.1f; 
    public float attackDelay = 0.2f;
    public Transform attackOrigin;
    public float radius = 0.3f;
    public int damage = 1;
    public float knockbackMultiplier = 3f;
    //balls
    public bool attackBlocked;
    public bool isSword;
    public bool isStaff;

    public Animator animator;
    public SpriteRenderer weaponRenderer;


    public void ResetAttackBlocked()
    {
        attackBlocked = false;
    }

    public IEnumerator DelayAttack()
    {
        yield return new WaitForSeconds(attackDelay);
        attackBlocked = false;
    }

    public void DetectColliders()
    {
        if(isSword == true)
        {
            FindObjectOfType<BryceAudioManager>().Play("Dagger");
        } else if(isStaff == true)
        {
            FindObjectOfType<BryceAudioManager>().Play("Magma");
        }
        else
        {
            FindObjectOfType<BryceAudioManager>().Play("Sword");
        }
        StartCoroutine(DelayedDetection());
    }

    public void ChangeScale(float scaleAmount)
    {
        transform.localScale += new Vector3(scaleAmount, scaleAmount, scaleAmount);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 position = attackOrigin == null ? Vector3.zero : attackOrigin.position;
        Gizmos.DrawWireSphere(position, radius);
    }

    public IEnumerator DelayedDetection()
    {
        yield return new WaitForSeconds(detectionDelay);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackOrigin.position, radius);

        foreach (Collider2D collider in colliders)
        {
            //Debug.Log("hit an enemy");
            KnockbackFeedback knockbackScript = collider.GetComponent<KnockbackFeedback>();
            //if (knockbackScript != null)
            //{
                knockbackScript.SetKnockbackMultiplier(knockbackMultiplier);
                knockbackScript.PlayFeedback(gameObject);
            //}

            Health healthComponent = collider.GetComponent<Health>();
            //if (healthComponent != null)
            //{
                healthComponent.GetHit(damage, transform.parent.gameObject);
            //}
        }
    }
}

