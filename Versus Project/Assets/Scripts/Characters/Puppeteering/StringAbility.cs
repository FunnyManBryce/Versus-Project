using Unity.Netcode;
using UnityEngine;


public class StringAbility : NetworkBehaviour
{
    public int team;
    public float damage;
    public float markAmount;
    public Transform Pos;
    public NetworkObject sender;
    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            GameObject camera = GameObject.Find("Main Camera");
            Vector3 mousePosition = camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x, mousePosition.y, 0);
        }
        //damage = sender.GetComponent<BasePlayerController>().attackDamage * 2;
    }

    public void Damage()
    {
        if (!IsServer) return;
        Vector2 pos = new Vector2(Pos.position.x, Pos.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 4);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                if (collider.gameObject.tag != "Tower" && collider.gameObject.tag != "Inhibitor")
                {
                    collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen, false);
                    if (!collider.GetComponent<NetworkObject>().IsSpawned == false)
                    {
                        InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Marked", markAmount, 10f, true);
                        if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                        {
                            InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Speed", -2, 5f, true);
                        }
                    }
                }
            }
        }
    }

    public void Despawn()
    {
        if (!IsServer) return;
        gameObject.GetComponent<NetworkObject>().Despawn();
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

