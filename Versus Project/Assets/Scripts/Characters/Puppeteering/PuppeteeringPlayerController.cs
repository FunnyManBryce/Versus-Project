using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PuppeteeringPlayerController : BasePlayerController
{
    public LameManager lameManager;
    public GameObject Puppet;
    public GameObject currentPuppet;
    public NetworkVariable<bool> puppetAlive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> puppetDeathTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //Ability 1
    private bool isAOneOnCD;
    public float AOneManaCost;
    public float abilityOneCD;
    public GameObject stringObject;

    //Ability 2
    private bool isATwoOnCD;
    public float ATwoManaCost;
    public float abilityTwoCD;
    public float shockwaveDamage;
    public GameObject shockwaveProjectile;

    //Ultimate Ability
    private bool isUltOnCD;
    public float ultManaCost;
    public float ultCD;
    private BasePlayerController enemyPlayer;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        lameManager = FindObjectOfType<LameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (IsOwner)
        {
            if (Input.GetKey(KeyCode.Q) && isAOneOnCD == false && AOneManaCost <= mana)
            {
                isAOneOnCD = true;
                mana -= AOneManaCost;
                IEnumerator coroutine = CooldownTimer(abilityOneCD / (cDR / 2), 1);
                StartCoroutine(coroutine);
                StringSummonServerRpc();
            }
            if (Input.GetKey(KeyCode.E) && isATwoOnCD == false && ATwoManaCost <= mana)
            {
                isATwoOnCD = true;
                mana -= ATwoManaCost;
                IEnumerator coroutine = CooldownTimer(abilityTwoCD / (cDR / 2), 2);
                StartCoroutine(coroutine);
                PuppetModeSwitchServerRpc();
            }
            if (Input.GetKey(KeyCode.R) && isUltOnCD == false && ultManaCost <= mana)
            {
                isUltOnCD = true;
                mana -= ultManaCost;
                IEnumerator coroutine = CooldownTimer(ultCD / (cDR / 2), 3);
                StartCoroutine(coroutine);
                UltimateServerRpc();
            }
            if (puppetAlive.Value == false)
            {
                float currentTime = lameManager.matchTimer.Value;
                if (currentTime - puppetDeathTime.Value >= 15f)
                {
                    PuppetSpawnServerRpc(teamNumber.Value, attackDamage, maxSpeed);
                }
            } else
            {
                if(currentTarget != null)
                {
                    SyncPuppetValuesServerRpc(currentTarget);
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            int team = NetworkManager.LocalClientId == 0 ? 1 : 2;
            SetTeamServerRpc(team);

            string canvasName = NetworkManager.LocalClientId == 0 ? "Player1UICanvas" : "Player2UICanvas";
            GameObject playerCanvas = GameObject.Find(canvasName);

            if (playerCanvas != null)
            {
                GameObject healthBar = Instantiate(healthBarPrefab, playerCanvas.transform);
                healthBar.GetComponent<PlayerHealthBar>().enabled = true;
            }
            PuppetSpawnServerRpc(team, attackDamage, maxSpeed);
        }
    }

    [Rpc(SendTo.Server)]
    private void PuppetSpawnServerRpc(int team, float damage, float speed)
    {
        if(puppetAlive.Value == false)
        {
            puppetAlive.Value = true;
            currentPuppet = Instantiate(Puppet, gameObject.transform.position, Quaternion.identity);
            currentPuppet.GetComponent<Puppet>().Team = team;
            currentPuppet.GetComponent<Puppet>().health.Team.Value = team;
            currentPuppet.GetComponent<Puppet>().Father = gameObject;
            currentPuppet.GetComponent<Puppet>().Damage = 1.5f * damage;
            currentPuppet.GetComponent<Puppet>().moveSpeed = 1f * speed;
            var puppetNetworkObject = currentPuppet.GetComponent<NetworkObject>();
            puppetNetworkObject.Spawn();
        }
    }

    [Rpc(SendTo.Server)]
    private void SyncPuppetValuesServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObj))
        {
            currentTarget = targetObj;
        }
    }

    [Rpc(SendTo.Server)]
    private void StringSummonServerRpc()
    {
        var String = Instantiate(stringObject, gameObject.transform.position, Quaternion.identity);
        String.GetComponent<StringAbility>().team = teamNumber.Value;
        String.GetComponent<StringAbility>().sender = gameObject.GetComponent<NetworkObject>();
        var StringNetworkObject = String.GetComponent<NetworkObject>();
        StringNetworkObject.SpawnWithOwnership(clientID);
    }

    [Rpc(SendTo.Server)]
    private void UltimateServerRpc()
    {

    }

    [Rpc(SendTo.Server)]
    private void PuppetModeSwitchServerRpc()
    {

    }

    public IEnumerator CooldownTimer(float duration, int abilityNumber)
    {
        yield return new WaitForSeconds(duration);
        if (abilityNumber == 1)
        {
            isAOneOnCD = false;
        }
        else if (abilityNumber == 2)
        {
            isATwoOnCD = false;
        }
        else if (abilityNumber == 3)
        {
            isUltOnCD = false;
        }
        Debug.Log("Ability Off Cooldown");
    }
}
