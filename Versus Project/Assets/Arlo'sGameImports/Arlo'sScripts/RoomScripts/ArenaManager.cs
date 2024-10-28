using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    public int Stage;
    public int Wave;
    public int realWaveNumber;
    static public int enemiesAlive;
    int enemiesToSpawn;
    public int[] enemiesPerWave;
    public List<GameObject> enemies;
    public GameObject[] spawnPoints;
    public GameObject Skeleton;
    public GameObject FireBlob;
    public GameObject Pyromancer;
    public GameObject lavaGolem;
    public GameObject boss1;
    public GameObject boss2;
    public GameObject victoryPage;
    bool waveStarted = false;
    bool bossAlive;
    float spawnTimer;
    void Start()
    {
        enemiesAlive = 0; //code might screw me over later we'll see
        enemiesToSpawn = enemiesPerWave[Wave];
        waveStarted = true;
        spawnTimer = Random.Range(1, 2);
        FindObjectOfType<BryceAudioManager>().Play("Skeleton Theme");
    }

    void Update()
    {
        
        if (enemiesToSpawn > 0 && spawnTimer <= 0)
        {
            Instantiate(enemies[Random.Range(0,enemies.Capacity)], spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
            enemiesToSpawn--;
            enemiesAlive++;
            spawnTimer = Random.Range(1, 3);
            //Debug.Log("enemies to spawn" + enemiesToSpawn);
        }
        else if (waveStarted == true)
        {
            spawnTimer -= Time.deltaTime;
        }
        if(waveStarted == true && enemiesAlive == 0 && enemiesToSpawn == 0)
        {
            Wave++;
            realWaveNumber++;
            //Debug.Log("Wave Number: " + Wave);
            enemiesToSpawn = enemiesPerWave[Wave];
            waveStarted = true;
            spawnTimer = Random.Range(1, 2);
            if (Wave == 4)
            {
                Instantiate(boss1, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                FindObjectOfType<BryceAudioManager>().Play("Boss Spawn");
                FindObjectOfType<BryceAudioManager>().Play("Boss Theme");
                FindObjectOfType<BryceAudioManager>().Stop("Skeleton Theme");
            }
            if (Wave == 5)
            {
                enemies.Add(FireBlob);
                Instantiate(FireBlob, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                FindObjectOfType<BryceAudioManager>().Play("Fire Theme");
                FindObjectOfType<BryceAudioManager>().Stop("Boss Theme");
            }
            if(Wave == 6)
            {
                enemies.Add(FireBlob);
                Instantiate(FireBlob, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                Instantiate(FireBlob, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 7)
            {
                enemies.Add(FireBlob);
                enemies.Add(Pyromancer);
                Instantiate(Pyromancer, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 8)
            {
                enemies.Add(Pyromancer);
                Instantiate(Pyromancer, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                Instantiate(Pyromancer, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 9)
            {
                enemies.Add(lavaGolem);
                Instantiate(lavaGolem, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if(Wave == 10)
            {
                enemies.Remove(Skeleton);
                Instantiate(lavaGolem, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if(Wave == 11)
            {
                enemies.Remove(FireBlob);
                Instantiate(lavaGolem, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                Instantiate(Pyromancer, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 12)
            {
                enemies.Remove(FireBlob);
                enemies.Remove(Skeleton);
                Instantiate(boss1, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 13)
            {
                Instantiate(boss1, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                Instantiate(boss1, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
            }
            if (Wave == 14)
            {
                Instantiate(boss2, spawnPoints[Random.Range(0, 4)].transform.position, Quaternion.identity);
                enemiesAlive++;
                FindObjectOfType<BryceAudioManager>().Play("Boss Spawn");
                FindObjectOfType<BryceAudioManager>().Play("Boss Theme");
                FindObjectOfType<BryceAudioManager>().Stop("Fire Theme");
            }
            if(Wave == 15)
            {
                //go to victory scene
                victoryPage.SetActive(true);
                //Debug.Log("huh");
            }
           
        }
    }


}
