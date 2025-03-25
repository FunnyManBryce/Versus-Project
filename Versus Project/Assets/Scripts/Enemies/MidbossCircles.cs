using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode;

public class MidbossCircles : NetworkBehaviour
{
    public float damage;
    public float lifespan = 0.5f;
    public NetworkObject sender;

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
        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 5);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && collider.isTrigger && collider.tag != "JungleEnemy")
            {
                Debug.Log("Damage Triggering");
                collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), 5, false);
            }
        }
    }

    
}
