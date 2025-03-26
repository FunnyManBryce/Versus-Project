using Unity.Netcode;
using UnityEngine;


public class StringAbility : NetworkBehaviour
{
    public int team;
    public float damage;
    public float markAmount;
    public Transform Pos;
    public float lifespan = 0.5f;
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
        Debug.Log("string do thing");
        Vector2 pos = new Vector2(Pos.position.x, Pos.position.y);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, 4);
        foreach (var collider in hitColliders)
        {
            if (collider.GetComponent<Health>() != null && CanAttackTarget(collider.GetComponent<NetworkObject>()) && collider.isTrigger)
            {
                if (collider.gameObject.tag == "Tower" && !sender.GetComponent<PuppeteeringPlayerController>().stringTargetsAll || collider.gameObject.tag == "Inhibitor" && !sender.GetComponent<PuppeteeringPlayerController>().stringTargetsAll)
                {
                    return;
                }
                else
                {
                    Debug.Log("Damage Triggering");
                    collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Immobilized", 0f, 0.5f, true);
                    InflictBuffServerRpc(collider.GetComponent<NetworkObject>(), "Marked", markAmount, 5f, true);
                    if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                    {
                        collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", -2, 5f, true);
                    }
                }
                /*if (collider.GetComponent<BasePlayerController>() != null)
                {
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f, true);
                    collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Marked", markAmount, 5f, true);
                    if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                    {
                        collider.GetComponent<BasePlayerController>().TriggerBuffServerRpc("Speed", -3, 5f, true);
                    }
                }
                if (collider.GetComponent<MeleeMinion>() != null)
                {
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Marked", markAmount, 5f);
                    if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                    {
                        collider.GetComponent<MeleeMinion>().TriggerBuffServerRpc("Speed", -1, 5f);
                    }
                }
                if (collider.GetComponent<JungleEnemy>() != null)
                {
                    Debug.Log("Effect Triggering");
                    collider.GetComponent<JungleEnemy>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    collider.GetComponent<JungleEnemy>().TriggerBuffServerRpc("Marked", markAmount, 5f);
                    if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                    {
                        collider.GetComponent<JungleEnemy>().TriggerBuffServerRpc("Speed", -1, 5f);
                    }
                }
                if (collider.GetComponent<Puppet>() != null)
                {
                    collider.GetComponent<Puppet>().TriggerBuffServerRpc("Immobilized", 0f, 0.5f);
                    collider.GetComponent<Puppet>().TriggerBuffServerRpc("Marked", markAmount, 5f);
                    if (sender.GetComponent<PuppeteeringPlayerController>().stringMoveReduction)
                    {
                        collider.GetComponent<Puppet>().TriggerBuffServerRpc("Speed", -1, 5f);
                    }
                }
                if (!sender.GetComponent<PuppeteeringPlayerController>().stringTargetsAll && collider.GetComponent<Tower>() != null)
                {
                    collider.GetComponent<Tower>().TriggerBuffServerRpc("Marked", markAmount, 5f);
                }*/
            }
        }
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

    [ServerRpc]
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

