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
            transform.rotation = Quaternion.Euler(0, 0, rotZ - 90);
            //ChangeRotationServerRpc(rotZ);
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            transform.Translate(Vector2.up * speed * Time.deltaTime);
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
            collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen);
            //gameObject.GetComponent<NetworkObject>().Despawn();
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
