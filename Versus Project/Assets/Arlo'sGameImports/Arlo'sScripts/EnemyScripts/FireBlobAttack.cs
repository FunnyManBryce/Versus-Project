using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBlobAttack : MonoBehaviour
{
    //PyromancerAI pyromancerAI;
    SkeletonBossProjectile skeletonBossProjectile;
    public GameObject fireBlob;
    public GameObject projectile;
    public Transform enemyPosition;
    public int damage;
    static public Vector3 projectileRotation;
    public bool SpawnCooldown = true;
    //public float attackDelay = 0.01f;
    // Start is called before the first frame update
    void Start()
    {
        skeletonBossProjectile = projectile.GetComponent<SkeletonBossProjectile>();
        StartCoroutine(SpawnDelay());
    }

    // Update is called once per frame
    void Update()
    {
      
    }
    
    public void Death()
    {
        FindObjectOfType<BryceAudioManager>().Play("Fire");
        //Invoke("DeathAttack", attackDelay);
        projectileRotation = new Vector3(0, 0, 90);
        Quaternion newRotation = Quaternion.Euler(projectileRotation);
        Instantiate(projectile, new Vector3(enemyPosition.position.x, enemyPosition.position.y ), newRotation);
        projectileRotation = new Vector3(0, 0, -90);
        newRotation = Quaternion.Euler(projectileRotation);
        Instantiate(projectile, new Vector3(enemyPosition.position.x, enemyPosition.position.y), newRotation);
        projectileRotation = new Vector3(0, 0, 0);
        newRotation = Quaternion.Euler(projectileRotation);
        Instantiate(projectile, new Vector3(enemyPosition.position.x + 1, enemyPosition.position.y), newRotation);
        projectileRotation = new Vector3(0, 0, 180);
        newRotation = Quaternion.Euler(projectileRotation);
        Instantiate(projectile, new Vector3(enemyPosition.position.x - 1, enemyPosition.position.y), newRotation);
    }

    /*public void SummonDeath()
    {
        lavaBoss = GameObject.Find("Gerald").GetComponent<LavaBoss>();
        lavaBoss.currentSummons--;
    }*/

    

    private void OnCollisionStay2D(Collision2D col)
    {
     
        if (col.gameObject.tag == "Player" && SpawnCooldown == false)
        {
            Health health;
            health = col.gameObject.GetComponent<Health>();
            health.GetHit(damage, fireBlob);
        }
    }

    public IEnumerator SpawnDelay()
    {
        yield return new WaitForSeconds(0.75f);
        SpawnCooldown = false;
    }
}
