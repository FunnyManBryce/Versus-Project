using Unity.Netcode;
using UnityEngine;

public class ProjectileController : NetworkBehaviour
{
    public float speed;
    public float damage;
    private NetworkObject target;
    private NetworkObject sender;
    private bool isTargetDestroyed = false;

    public void Initialize(float projSpeed, float projDamage, NetworkObject targetObj, NetworkObject senderObj)
    {
        speed = projSpeed;
        damage = projDamage;
        target = targetObj;
        sender = senderObj;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (target == null || !target.IsSpawned)
        {
            if (!isTargetDestroyed)
            {
                isTargetDestroyed = true;
                DestroyProjectileServerRpc();
            }
            return;
        }

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        transform.Translate(direction * speed * Time.deltaTime);
        float distanceToTarget = Vector2.Distance(transform.position, target.transform.position);
        if (distanceToTarget < 0.5f)
        {
            HandleCollision();
        }
    }

    private void HandleCollision()
    {
        if (target == null || !target.IsSpawned) return;

        target.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), 0);
        DestroyProjectileServerRpc();
    }

    [ServerRpc]
    private void DestroyProjectileServerRpc()
    {
        NetworkObject.Despawn();
        Destroy(gameObject);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        target = null;
        sender = null;
    }
}