using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaGolemAttack : EnemyMeleeWeapon
{
    public GameObject shockwave;
    public GameObject weaponParent;
    //public Shockwave shockwaveScript;
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

    public void Shockwave()
    {
        FindObjectOfType<BryceAudioManager>().Play("Golem Fire");
        Instantiate(shockwave, new Vector3(enemyAttackOrigin.position.x, enemyAttackOrigin.position.y), weaponParent.transform.rotation);
    }
    
}
