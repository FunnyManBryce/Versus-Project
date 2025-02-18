using Unity.Netcode;
using UnityEngine;

public class ProjectileController : NetworkBehaviour
{
    public float speed;
    public float damage;
    public float armorPen;
    private NetworkObject target;
    private NetworkObject sender;
    private bool isTargetDestroyed = false;
    private BasePlayerController player;

    public void Initialize(float projSpeed, float projDamage, NetworkObject targetObj, NetworkObject senderObj, float AP)
    {
        speed = projSpeed;
        damage = projDamage;
        target = targetObj;
        sender = senderObj;
        armorPen = AP;
        if(target.CompareTag("Player"))
        {
            player = target.GetComponent<BasePlayerController>();
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (target == null || !target.IsSpawned || target.CompareTag("Player") && player.isDead.Value)
        {
            if (!isTargetDestroyed)
            {
                isTargetDestroyed = true;
                DestroyProjectileServerRpc();
            }
            return;
        }

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotZ - 90);
        transform.Translate(Vector2.up * speed * Time.deltaTime);
        float distanceToTarget = Vector2.Distance(transform.position, target.transform.position);
        if (distanceToTarget < 0.5f)
        {
            HandleCollision();
        }
    }

    private void HandleCollision()
    {
        if (target == null || !target.IsSpawned) return;

        if(sender != null)
        {
            target.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), armorPen);
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