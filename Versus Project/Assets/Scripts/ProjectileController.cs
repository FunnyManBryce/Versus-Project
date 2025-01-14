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

        if (target.CompareTag("Player"))
        {
            target.GetComponent<BasePlayerController>().TakeDamageServerRpc(damage, new NetworkObjectReference(sender));
        }
        else if (target.CompareTag("Tower"))
        {
            target.GetComponent<Tower>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender));
        }
        else if (target.CompareTag("Minion"))
        {
            target.GetComponent<MeleeMinion>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender));
        }
        else if (target.CompareTag("Inhibitor"))
        {
            target.GetComponent<Inhibitor>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender));
        }
        else if (target.CompareTag("JungleEnemy"))
        {
            target.GetComponent<JungleEnemy>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender));
        }

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