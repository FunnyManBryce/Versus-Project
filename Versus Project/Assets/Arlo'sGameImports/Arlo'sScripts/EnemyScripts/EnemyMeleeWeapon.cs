using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeWeapon : MonoBehaviour
{
    public Enemy enemyScript;
    public GameObject enemy;
    public GameObject weapon;
    public Transform target;
    public Vector2 playerPosition;
    public Transform enemyAttackOrigin;
    public float enemyRadius;
    public int enemyDamage;
    public float attackDuration;
    public float attackCooldown;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        enemyScript = enemy.GetComponent<Enemy>();
        weapon.GetComponent<Renderer>().enabled = false;
        player = GameObject.FindGameObjectWithTag("Player");
        target = player.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyScript.isAttacking == false)
        {
        playerPosition = new Vector2(target.position.x, target.position.y);

        Vector2 direction = ((playerPosition - (Vector2)transform.position).normalized);

        transform.right = direction;

        Vector2 scale = transform.localScale;
 
        transform.localScale = scale;
        }
    }
    
    public void EnemyAttack()
    {
        weapon.GetComponent<Renderer>().enabled = true;
        StartCoroutine(DelayAttack());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 position = enemyAttackOrigin == null ? Vector3.zero :enemyAttackOrigin.position;
        Gizmos.DrawWireSphere(position, enemyRadius);
    }

    public void DetectColliders()
    {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(enemyAttackOrigin.position, enemyRadius))
        {
            //Debug.Log(collider.name);
            Health health;
            if (health = collider.GetComponent<Health>())
            {
                health.GetHit(enemyDamage, transform.parent.gameObject);
            }
        }
    }

    public IEnumerator DelayAttack()
    {
        yield return new WaitForSeconds(attackDuration);
        enemyScript.isAttacking = false;
        weapon.GetComponent<Renderer>().enabled = false;
        enemyScript.cooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        enemyScript.cooldown = false;
    }
}
