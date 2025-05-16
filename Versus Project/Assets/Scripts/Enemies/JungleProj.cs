using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class JungleProj : NetworkBehaviour
{
    public Transform ProjPos;
    public float speed = 5f;
    public float damage;
    public Vector3 target;
    public NetworkObject sender;
    public float lifespan = 2f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Vector3 rotation = target - transform.position;
            float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }
    }
    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer) return;
        if (collider.GetComponent<BasePlayerController>() != null && collider.isTrigger || collider.GetComponent<Puppet>() != null && collider.isTrigger)
        {
            collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<JungleEnemy>().armorPen, false);
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        transform.Translate(Vector2.right * speed * Time.deltaTime);
        lifespan -= Time.deltaTime;
        if (lifespan < 0 || sender == null)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
        }

    }
}
