using Unity.Netcode;
using UnityEngine;

public class ProjectileController : NetworkBehaviour
{
    public float speed;
    public float damage;
    public float armorPen;
    public float acceleration = 1;
    private NetworkObject target;
    private NetworkObject sender;
    private bool isTargetDestroyed = false;
    private BasePlayerController player;
    public bool appliesDarkness;

    public void Initialize(float projSpeed, float projDamage, NetworkObject targetObj, NetworkObject senderObj, float AP)
    {
        speed = projSpeed;
        damage = projDamage;
        target = targetObj;
        sender = senderObj;
        armorPen = AP;
        if (target.CompareTag("Player"))
        {
            player = target.GetComponent<BasePlayerController>();
            if(player.appliesDarkness.Value)
            {
                appliesDarkness = true;
            }
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

        speed += Time.deltaTime * acceleration;
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

        if (sender != null)
        {
            target.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), armorPen, false);
            if(appliesDarkness && target.GetComponent<Health>().darknessEffect == false)
            {
                InflictBuffServerRpc(new NetworkObjectReference(target), "Darkness", -1, 5, true);
            }
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

    [ServerRpc(RequireOwnership = false)]
    public void InflictBuffServerRpc(NetworkObjectReference Target, string buffType, float amount, float duration, bool hasDuration)
    {
        if (Target.TryGet(out NetworkObject targetObj))
        {
            if (targetObj.GetComponent<BasePlayerController>() != null)
            {
                targetObj.GetComponent<BasePlayerController>().TriggerBuffServerRpc(buffType, amount, duration, hasDuration);
            }
            if (targetObj.GetComponent<MeleeMinion>() != null)
            {
                targetObj.GetComponent<MeleeMinion>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<Puppet>() != null)
            {
                targetObj.GetComponent<Puppet>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<JungleEnemy>() != null)
            {
                targetObj.GetComponent<JungleEnemy>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<Tower>() != null)
            {
                targetObj.GetComponent<Tower>().TriggerBuffServerRpc(buffType, amount, duration);
            }
            if (targetObj.GetComponent<MidBoss>() != null)
            {
                targetObj.GetComponent<MidBoss>().TriggerBuffServerRpc(buffType, amount, duration);
            }
        }
    }
}