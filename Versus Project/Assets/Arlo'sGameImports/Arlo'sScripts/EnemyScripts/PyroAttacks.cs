using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PyroAttacks : EnemyMeleeWeapon
{
    public PyromancerAI pyromancerScript;
    public GameObject fireball;
    public GameObject weaponParent;
    public GameObject fireBlob;
    public GameObject Object;
    public GameObject pyro;
    public Transform spawnPoint;
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
            weapon.GetComponent<Renderer>().enabled = false;
    }

    public void Fireball()
    {
        FindObjectOfType<BryceAudioManager>().Play("Fire");
        Instantiate(fireball, new Vector3(enemyAttackOrigin.position.x, enemyAttackOrigin.position.y), weaponParent.transform.rotation);
    }

    public void Summon()
    {
        FindObjectOfType<BryceAudioManager>().Play("Boss Summon");
        spawnPoint = Object.GetComponent<Transform>();
        GameObject fireBlob1 = Instantiate(fireBlob, new Vector3(spawnPoint.position.x, spawnPoint.position.y), Quaternion.identity);
        fireBlob1.GetComponent<Enemy>().Creator = pyro;
        ArenaManager.enemiesAlive++;
        pyromancerScript.isSummoning = false;
        StartCoroutine(DelayAttack());
        Debug.Log("erm");
    }
}
