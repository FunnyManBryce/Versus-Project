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
    public Transform AOEPos;
    public float lifespan = 5f;
    public NetworkObject sender;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = new Vector3(0, 0, 0);
        if (!IsServer) return;
        lifespan -= Time.deltaTime;
        if(lifespan < 0)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
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
        Vector2 pos = new Vector2(AOEPos.position.x, AOEPos.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 8);
        foreach (var collider in hitColliders)
        {

            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(damagePerTick, new NetworkObjectReference(sender), 0);
                if(collider.GetComponent<BasePlayerController>() != null)
                {
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", speedReductionPerTick, 5f);
                }
                if (collider.GetComponent<MeleeMinion>() != null)
                {
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Speed", speedReductionPerTick/2f, 5f);
                }
            }
        }
    }

    private bool CanAttackTarget(NetworkObject targetObject)
    {
        // Check if target has a team component
        if (targetObject.TryGetComponent(out BasePlayerController targetPlayer))
        {
            return targetPlayer.teamNumber.Value != team;
        }

        if (targetObject.TryGetComponent(out Tower targetTower))
        {
            return targetTower.Team != team;
        }

        if (targetObject.TryGetComponent(out MeleeMinion targetMinion))
        {
            return targetMinion.Team != team;
        }

        if (targetObject.TryGetComponent(out Inhibitor targetInhibitor))
        {
            return targetInhibitor.Team != team;
        }

        return true; // Default to allowing attack if no team check is possible
    }
}
