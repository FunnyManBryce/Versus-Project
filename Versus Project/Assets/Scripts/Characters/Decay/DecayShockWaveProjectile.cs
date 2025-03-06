using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class DecayShockWaveProjectile : NetworkBehaviour
{
    public int team;
    public Transform ProjPos;
    public float speed = 3f;
    public float lifespan = 2f;
    public float damage;
    public NetworkObject sender;
    public Vector2 target;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameObject camera = GameObject.Find("Main Camera");
            Vector3 mousePosition = camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            Vector3 rotation = mousePosition - transform.position;
            float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotZ);
            //ChangeRotationServerRpc(rotZ);
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
        }
        if (!IsServer) return;
        lifespan -= Time.deltaTime;
        if (lifespan < 0)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer) return;
        if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
        {
            collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen, true);
            if(sender.GetComponent<DecayPlayerController>().immobilizeShockwave)
            {
                if (collider.GetComponent<BasePlayerController>() != null)
                {
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f, true);
                }
                if (collider.GetComponent<MeleeMinion>() != null)
                {
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                }
                if (collider.GetComponent<JungleEnemy>() != null)
                {
                    collider.GetComponent<JungleEnemy>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                }
                if (collider.GetComponent<Puppet>() != null)
                {
                    collider.GetComponent<Puppet>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void ChangeRotationServerRpc(float rotZ)
    {
        transform.rotation = Quaternion.Euler(0, 0, rotZ - 90);
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
