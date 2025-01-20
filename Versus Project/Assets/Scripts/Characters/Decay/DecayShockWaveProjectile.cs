using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class DecayShockWaveProjectile : NetworkBehaviour
{
    public int team;
    public Transform ProjPos;
    public float speed = 3f;
    public float lifespan = 2f;
    public float damage;
    public NetworkObject sender;
    public Vector2 target;

    private void Awake()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 rotation = mousePosition - transform.position;
        float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotZ - 90);
        //gameObject.transform.LookAt(new Vector3(0,0,Input.GetAxis("Mouse Z")));
        //target = mousePosition;

    }

    void Update()
    {
        if (!IsServer) return;
        transform.Translate(Vector2.up * speed * Time.deltaTime);
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
            collider.GetComponent<Health>().TakeDamageServerRPC(damage, new NetworkObjectReference(sender), sender.GetComponent<BasePlayerController>().armorPen);
            //gameObject.GetComponent<NetworkObject>().Despawn();
        }
    } 

    private bool CanAttackTarget(NetworkObject targetObject)
    {
        // Check if target has a team component
        if (targetObject.TryGetComponent(out BasePlayerController targetPlayer))
        {
            return targetPlayer.teamNumber.Value != team;
        }

        if (targetObject.TryGetComponent(out Tower targetTower))
        {
            return targetTower.Team != team;
        }

        if (targetObject.TryGetComponent(out MeleeMinion targetMinion))
        {
            return targetMinion.Team != team;
        }

        if (targetObject.TryGetComponent(out Inhibitor targetInhibitor))
        {
            return targetInhibitor.Team != team;
        }

        return true; // Default to allowing attack if no team check is possible
    }
}
