using Unity.Netcode;
using UnityEngine;

public class MidbossCircles : NetworkBehaviour
{
    public float damage;
    public float lifespan = 0.5f;
    public NetworkObject sender;

    // Update is called once per frame
    void Update()
    {
        /*if (!IsServer) return;
        lifespan -= Time.deltaTime;
        if (lifespan < 0)
        {
            Damage();
            gameObject.GetComponent<NetworkObject>().Despawn();
        } */
    }

    public void DamageServerCheck()
    {
        if (!IsServer) return;
        Damage();

    }

    public void DespawnServerCheck()
    {
        if (!IsServer) return;
        gameObject.GetComponent<NetworkObject>().Despawn();
    }
    public void Damage()
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 5f);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && collider.isTrigger && collider.tag != "JungleEnemy")
            {
                Debug.Log("Damage Triggering");
                collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<MidBoss>().armorPen, false);
            }
        }
    }


}
