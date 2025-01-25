using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class StringAbility : NetworkBehaviour
{
    public int team;
    public float damage;
    public float markAmount;
    public Transform Pos;
    public float lifespan = 0.5f;
    public NetworkObject sender;
    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            GameObject camera = GameObject.Find("Main Camera");
            Vector3 mousePosition = camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            transform.position = mousePosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        lifespan -= Time.deltaTime;
        if (lifespan < 0)
        {
            Damage();
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }

    void Damage()
    {
        Vector2 pos = new Vector2(Pos.position.x, Pos.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 3);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen);
                if (collider.GetComponent<BasePlayerController>() != null)
                {
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Immobilized", 0f, 1f);
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Marked", markAmount, 5f);
                }
                if (collider.GetComponent<MeleeMinion>() != null)
                {
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Immobilized", 0f, 1f);
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Marked", markAmount, 5f);
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
}

