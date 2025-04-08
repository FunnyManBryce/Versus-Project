using Unity.Netcode;
using UnityEngine;


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
            if (sender.GetComponent<DecayPlayerController>().immobilizeShockwave)
            {
                InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Immobilized", 0f, 0.5f, true);
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
