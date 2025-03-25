using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class DecayAOE : NetworkBehaviour
{
    public int team;
    public float lastTick;  
    public float damagePerTick;
    public float speedReductionPerTick;
    public float reductionDuration = 5f;
    public Transform AOEPos;
    public float lifespan = 5f;
    public NetworkObject sender;
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = new Vector3(0.01f, -0.1f, 0);
        if (!IsServer) return;
        lifespan -= Time.deltaTime;
        if(lifespan < 0)
        {
            animator.SetTrigger("AbilityOver");
        }
        float currentTime = Time.time;
        if (currentTime - lastTick >= 0.5f)
        {
            lastTick = currentTime;
            AOEDamage();
        }
    }

    void AOEDamage()
    {
        if (lifespan < 0) return;
        Vector2 pos = new Vector2(AOEPos.position.x, AOEPos.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 8);
        foreach (var collider in hitColliders)
        {

            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                if (collider.gameObject.tag == "Tower" || collider.gameObject.tag == "Inhibitor")
                {
                    return;
                }
                else
                {
                    collider.GetComponent<Health>().TakeDamageServerRPC(damagePerTick, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen, false);
                }
                if(collider.GetComponent<BasePlayerController>() != null)
                {
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", speedReductionPerTick, reductionDuration, true);
                }
                if (collider.GetComponent<MeleeMinion>() != null)
                {
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Speed", speedReductionPerTick/2f, reductionDuration);
                }
                if (collider.GetComponent<Puppet>() != null)
                {
                    collider.GetComponent<Puppet>().TriggerBuffServerRpc("Speed", speedReductionPerTick, reductionDuration);
                }
                if(sender.GetComponent<DecayPlayerController>().AOESpeedSteal) //Decay gains some speed for every hit enemy if the ability is level 3
                {
                    sender.GetComponent<DecayPlayerController>().TriggerBuffServerRpc("Speed", -(speedReductionPerTick * 0.1f), reductionDuration, true);
                }
            }
        }
    }

    private bool CanAttackTarget(NetworkObject targetObject)
    {
        // Check if target has health component
        if (targetObject.TryGetComponent(out Health targetHealth))
        {
            if (targetHealth.Team.Value == 0)
            {
                return true;
            }
            else
            {
                return targetHealth.Team.Value != sender.GetComponent<Health>().Team.Value;
            }
        }
        else
        {
            return false;
        }
    }

    public void AOEDespawn()
    {
        gameObject.GetComponent<NetworkObject>().Despawn();
    }
}

